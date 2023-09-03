#nullable enable

using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;
using Screenbox.Core.ViewModels;
using System;
using System.Threading.Tasks;

namespace Screenbox.Commands;
internal class ShowPropertiesCommand : IRelayCommand<MediaViewModel>
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand<MediaViewModel> _asyncCommand;

    public ShowPropertiesCommand()
    {
        _asyncCommand = new AsyncRelayCommand<MediaViewModel>(ShowDialog);
    }

    public bool CanExecute(object? parameter)
    {
        return parameter != null && _asyncCommand.CanExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        if (parameter is MediaViewModel media)
            Execute(media);
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanExecute(MediaViewModel? parameter)
    {
        return parameter != null && _asyncCommand.CanExecute(parameter);
    }

    public void Execute(MediaViewModel? parameter)
    {
        if (parameter == null) return;
        _asyncCommand.Execute(parameter);
    }

    private async Task ShowDialog(MediaViewModel? parameter)
    {
        PropertiesDialog dialog = new()
        {
            Media = parameter
        };

        await dialog.ShowAsync();
    }
}
