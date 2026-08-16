#nullable enable
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Commands;

/// <summary>
/// Represents a command that reveals a media file in File Explorer,
/// opening the containing folder and selecting the file.
/// </summary>
internal sealed partial class RevealInFileExplorerCommand : IRelayCommand<MediaViewModel>
{
    public event EventHandler? CanExecuteChanged;

    private readonly AsyncRelayCommand<MediaViewModel> _asyncCommand;

    public RevealInFileExplorerCommand()
    {
        _asyncCommand = new AsyncRelayCommand<MediaViewModel>(RevealInFileExplorerAsync);
        _asyncCommand.CanExecuteChanged += (_, _) => NotifyCanExecuteChanged();
    }

    /// <inheritdoc/>
    public bool CanExecute(MediaViewModel? parameter)
    {
        return parameter?.Source is StorageFile
            || (parameter?.Source is Uri uri && uri.IsFile)
            || parameter?.IsFromLibrary == true;
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

    private async Task RevealInFileExplorerAsync(MediaViewModel? parameter)
    {
        if (parameter is null)
            return;

        var filesService = Ioc.Default.GetRequiredService<IFilesService>();

        if (parameter.Source is StorageFile file)
        {
            await filesService.OpenFileLocationAsync(file);
        }
        else if (!string.IsNullOrEmpty(parameter.Location))
        {
            // Load details to try to resolve the file from a URI or path.
            await parameter.LoadDetailsAsync(filesService);
            if (parameter.Source is StorageFile resolvedFile)
            {
                await filesService.OpenFileLocationAsync(resolvedFile);
            }
            else
            {
                await filesService.OpenFileLocationAsync(parameter.Location);
            }
        }
    }
}
