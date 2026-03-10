#nullable enable

using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

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

        // Subscribe to PropertyChanged so we can monitor async execution task completion.
        if (_innerCommand is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += OnInnerPropertyChanged;
        }
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _innerCommand.CanExecute(parameter);

    /// <inheritdoc/>
    public void Execute(object? parameter) => _innerCommand.Execute(parameter);

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    private void OnInnerCanExecuteChanged(object? sender, EventArgs e) =>
        CanExecuteChanged?.Invoke(this, e);

    private void OnInnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IAsyncRelayCommand.ExecutionTask)) return;
        if (_innerCommand is not IAsyncRelayCommand asyncCommand) return;
        if (asyncCommand.ExecutionTask is not { } task) return;

        _ = MonitorExecutionTaskAsync(task);
    }

    /// <summary>
    /// Awaits the given execution task and invokes <see cref="_onSuccess"/> on normal
    /// completion or <see cref="_onFailure"/> when the task faults.
    /// </summary>
    private async Task MonitorExecutionTaskAsync(Task task)
    {
        try
        {
            await task;
            _onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            _onFailure?.Invoke(e);
        }
    }
}
