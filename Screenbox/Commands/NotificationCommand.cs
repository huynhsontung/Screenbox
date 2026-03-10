#nullable enable

using System;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Screenbox.Commands;

/// <summary>
/// An <see cref="ICommand"/> that wraps an underlying command and raises optional callbacks
/// when execution completes successfully (<paramref name="onSuccess"/>) or fails with an
/// exception (<paramref name="onFailure"/>), similar to a JS Promise.
/// </summary>
/// <remarks>
/// All <see cref="ICommand"/> interactions — <see cref="CanExecute"/>, <see cref="Execute"/>,
/// and <see cref="CanExecuteChanged"/> — are relayed to and from the underlying command.
/// For async commands (<see cref="IAsyncRelayCommand"/>), <see cref="INotifyPropertyChanged"/>
/// is used to monitor <see cref="IAsyncRelayCommand.ExecutionTask"/> and detect task completion.
/// </remarks>
internal sealed class NotificationCommand : ICommand
{
    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    private readonly ICommand _innerCommand;
    private readonly Action? _onSuccess;
    private readonly Action<Exception>? _onFailure;

    /// <summary>
    /// Initializes a new <see cref="NotificationCommand"/> that delegates all interactions to
    /// <paramref name="innerCommand"/> and invokes optional callbacks when execution completes.
    /// </summary>
    /// <param name="innerCommand">The underlying command to wrap and relay to.</param>
    /// <param name="onSuccess">Optional action invoked when the command completes successfully.</param>
    /// <param name="onFailure">Optional action invoked with the exception when the command fails.</param>
    public NotificationCommand(ICommand innerCommand, Action? onSuccess = null, Action<Exception>? onFailure = null)
    {
        _innerCommand = innerCommand;
        _onSuccess = onSuccess;
        _onFailure = onFailure;

        _innerCommand.CanExecuteChanged += OnInnerCanExecuteChanged;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _innerCommand.CanExecute(parameter);

    /// <inheritdoc/>
    public async void Execute(object? parameter)
    {
        try
        {
            _innerCommand.Execute(parameter);
            if (_innerCommand is IAsyncRelayCommand { ExecutionTask: { } task })
            {
                await task;
            }

            _onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            _onFailure?.Invoke(ex);
        }
    }

    private void OnInnerCanExecuteChanged(object? sender, EventArgs e) =>
        CanExecuteChanged?.Invoke(this, e);
}
