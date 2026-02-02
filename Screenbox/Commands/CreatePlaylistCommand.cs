#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;
using Screenbox.Core.Contexts;
using Screenbox.Core.ViewModels;

namespace Screenbox.Commands;

internal sealed class CreatePlaylistCommand : IAsyncRelayCommand<MediaViewModel?>
{
    public Task? ExecutionTask => _internalCommand.ExecutionTask;

    public bool CanBeCanceled => _internalCommand.CanBeCanceled;

    public bool IsCancellationRequested => _internalCommand.IsCancellationRequested;

    public bool IsRunning => _internalCommand.IsRunning;

    private readonly AsyncRelayCommand<MediaViewModel?> _internalCommand;
    private readonly PlaylistsContext _playlistsContext;

    public CreatePlaylistCommand()
    {
        _internalCommand = new AsyncRelayCommand<MediaViewModel?>(CreatePlaylistAsync);
        _playlistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
    }

    public event PropertyChangedEventHandler PropertyChanged
    {
        add
        {
            _internalCommand.PropertyChanged += value;
        }

        remove
        {
            _internalCommand.PropertyChanged -= value;
        }
    }

    public event EventHandler CanExecuteChanged
    {
        add
        {
            _internalCommand.CanExecuteChanged += value;
        }

        remove
        {
            _internalCommand.CanExecuteChanged -= value;
        }
    }

    private async Task CreatePlaylistAsync(MediaViewModel? parameter)
    {
        var playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (string.IsNullOrWhiteSpace(playlistName))
            return;

        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = playlistName;
        if (parameter != null)
        {
            playlist.Items.Add(parameter);
        }

        await playlist.SaveAsync();

        // Assume sort by last updated
        _playlistsContext.Playlists.Insert(0, playlist);
    }

    public Task ExecuteAsync(MediaViewModel? parameter)
    {
        return _internalCommand.ExecuteAsync(parameter);
    }

    public Task ExecuteAsync(object? parameter)
    {
        return _internalCommand.ExecuteAsync(parameter);
    }

    public void Cancel()
    {
        _internalCommand.Cancel();
    }

    public bool CanExecute(MediaViewModel? parameter)
    {
        return _internalCommand.CanExecute(parameter);
    }

    public void Execute(MediaViewModel? parameter)
    {
        _internalCommand.Execute(parameter);
    }

    public void NotifyCanExecuteChanged()
    {
        _internalCommand.NotifyCanExecuteChanged();
    }

    public bool CanExecute(object parameter)
    {
        return _internalCommand.CanExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _internalCommand.Execute(parameter);
    }
}
