#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Commands;

/// <summary>
/// Represents a command that opens a media file with an external application
/// using the system Open With dialog.
/// </summary>
internal sealed class OpenWithCommand : IRelayCommand<MediaViewModel>
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand<MediaViewModel> _asyncCommand;

    public OpenWithCommand()
    {
        _asyncCommand = new AsyncRelayCommand<MediaViewModel>(OpenWithAsync);
        _asyncCommand.CanExecuteChanged += (_, _) => NotifyCanExecuteChanged();
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter)
    {
        return parameter is MediaViewModel media && CanExecute(media);
    }

    /// <inheritdoc/>
    public void Execute(object? parameter)
    {
        if (parameter is MediaViewModel media)
        {
            Execute(media);
        }
    }

    /// <inheritdoc/>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public bool CanExecute(MediaViewModel? parameter)
    {
        return parameter?.Source is StorageFile && _asyncCommand.CanExecute(parameter);
    }

    /// <inheritdoc/>
    public void Execute(MediaViewModel? parameter)
    {
        if (parameter == null) return;
        _asyncCommand.Execute(parameter);
    }

    private async Task OpenWithAsync(MediaViewModel? parameter)
    {
        if (parameter?.Source is not StorageFile file)
        {
            // This should not happen if CanExecute is working correctly
            return;
        }

        try
        {
            LauncherOptions options = new()
            {
                DisplayApplicationPicker = true
            };

            bool success = await Launcher.LaunchFileAsync(file, options);
            if (!success)
            {
                LogService.Log($"Failed to launch file with external application. File: {file.Path}. This could mean no application is available for this file type or the user cancelled the operation.");
            }
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }
}
