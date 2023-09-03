#nullable enable

using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;
using System;
using System.Threading.Tasks;

namespace Screenbox.Commands;
internal class OpenUrlCommand : IRelayCommand
{
    public IRelayCommand<Uri>? NextCommand { get; set; }

    private readonly AsyncRelayCommand _asyncCommand;

    public OpenUrlCommand()
    {
        _asyncCommand = new AsyncRelayCommand(OpenUrlAsync);
    }

    public bool CanExecute(object? parameter)
    {
        return _asyncCommand.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _asyncCommand.Execute(parameter);
    }

    public event EventHandler? CanExecuteChanged;
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task OpenUrlAsync()
    {
        Uri? uri = await OpenUrlDialog.GetUrlAsync();
        NextCommand?.Execute(uri);
    }
}
