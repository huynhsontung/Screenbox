#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableRecipient
{
    private readonly IFilesService _filesService;
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IFilesService filesService, IPlaylistService playlistService, PlaylistsContext playlistsContext)
    {
        _filesService = filesService;
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = displayName;
        await playlist.SaveAsync();

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
        Messenger.Send(new PlaylistCreatedNotificationMessage(displayName));
    }

    public async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newDisplayName)
    {
        await playlist.RenameAsync(newDisplayName);
        Messenger.Send(new PlaylistRenamedNotificationMessage(newDisplayName));
    }

    public async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        string playlistName = playlist.Name;
        await _playlistService.DeletePlaylistAsync(playlist.Id);
        Playlists.Remove(playlist);
        Messenger.Send(new PlaylistDeletedNotificationMessage(playlistName));
    }

    private static bool NotEmpty(PlaylistViewModel? playlist) => playlist?.ItemsCount > 0;

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private void Play(PlaylistViewModel playlistVm)
    {
        var playlist = playlistVm.ToPlaylist();
        Messenger.Send(new QueuePlaylistMessage(playlist, true));
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private void PlayNext(PlaylistViewModel playlistVm)
    {
        Messenger.SendPlayNext(playlistVm.Items);
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private void AddToQueue(PlaylistViewModel playlistVm)
    {
        Messenger.SendAddToQueue(playlistVm.Items);
    }

    [RelayCommand]
    private async Task ImportPlaylistAsync()
    {
        StorageFile? file = await _filesService.PickFileAsync(".m3u8", ".m3u");
        if (file is null) return;

        IReadOnlyList<MediaViewModel> items = await _playlistService.ImportPlaylistItemsAsync(file);
        if (items.Count == 0) return;

        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = file.DisplayName;
        await playlist.AddItemsAsync(items);
        Playlists.Insert(0, playlist);
        Messenger.Send(new PlaylistCreatedNotificationMessage(playlist.Name));
    }

    [RelayCommand]
    private async Task ExportPlaylistAsync(PlaylistViewModel? playlist)
    {
        if (playlist is null) return;

        StorageFile? file = await _filesService.PickSaveFileAsync(playlist.Name, ".m3u8");
        if (file is null) return;

        await _playlistService.ExportPlaylistItemsAsync(playlist.Items, file);
    }
}
