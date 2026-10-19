using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.Storage;

namespace Screenbox.Commands;

/// <summary>
/// Represents a command that opens a media file in File Explorer,
/// opening the containing folder and selecting the file.
/// </summary>
internal sealed partial class OpenInFileExplorerCommand : IRelayCommand<MediaViewModel>
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand<MediaViewModel> _asyncCommand;

    public OpenInFileExplorerCommand()
    {
        _asyncCommand = new AsyncRelayCommand<MediaViewModel>(OpenInFileExplorerAsync);
        _asyncCommand.CanExecuteChanged += (_, _) => NotifyCanExecuteChanged();
    }

    /// <inheritdoc/>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public bool CanExecute(MediaViewModel? parameter)
    {
        return DeviceInfoHelper.IsDesktop
            && (parameter?.Source is StorageFile
            || (parameter?.Source is Uri uri && uri.IsFile)
            || parameter?.IsFromLibrary == true);
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter)
    {
        return parameter is MediaViewModel media && CanExecute(media);
    }

    /// <inheritdoc/>
    public void Execute(MediaViewModel? parameter)
    {
        if (parameter is null) return;
        _asyncCommand.Execute(parameter);
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

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task OpenInFileExplorerAsync(MediaViewModel? parameter)
    {
        if (parameter is null)
            return;

        var filesService = Ioc.Default.GetRequiredService<IFilesService>();

        if (await parameter.EnsureStorageFileSourceAsync() is { } file)
        {
            await filesService.OpenFileLocationAsync(file);
        }
        else if (!string.IsNullOrEmpty(parameter.Location))
        {
            await filesService.OpenFileLocationAsync(parameter.Location);
        }
    }
}

