#nullable enable

using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Commands;

internal class OpenWithCommand : IRelayCommand<MediaViewModel>
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand<MediaViewModel> _asyncCommand;

    public OpenWithCommand()
    {
        _asyncCommand = new AsyncRelayCommand<MediaViewModel>(OpenWithAsync);
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

    private async Task OpenWithAsync(MediaViewModel? parameter)
    {
        if (parameter?.Source is not StorageFile file)
            return;

        LauncherOptions options = new()
        {
            DisplayApplicationPicker = true
        };

        await Launcher.LaunchFileAsync(file, options);
    }
}
