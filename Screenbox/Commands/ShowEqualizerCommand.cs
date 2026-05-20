#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Dialogs;

namespace Screenbox.Commands;

internal sealed class ShowEqualizerCommand : IRelayCommand
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand _asyncCommand;

    public ShowEqualizerCommand()
    {
        _asyncCommand = new AsyncRelayCommand(ShowDialogAsync);
    }

    public bool CanExecute(object? parameter)
    {
        return _asyncCommand.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _asyncCommand.Execute(parameter);
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ShowDialogAsync()
    {
        var dialog = new EqualizerDialog();
        await dialog.ShowAsync();
    }
}
