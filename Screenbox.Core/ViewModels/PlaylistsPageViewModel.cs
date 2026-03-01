#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableRecipient
{
    private readonly IPlaylistService _playlistService;
    private readonly IFilesService _filesService;
    private readonly PlaylistsContext _playlistsContext;
    private readonly MediaViewModelFactory _mediaFactory;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IPlaylistService playlistService, IFilesService filesService,
        PlaylistsContext playlistsContext, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _filesService = filesService;
        _playlistsContext = playlistsContext;
        _mediaFactory = mediaFactory;
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = displayName;
        await playlist.SaveAsync();

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
    }

    public async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newDisplayName)
    {
        await playlist.RenameAsync(newDisplayName);
    }

    public async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        await _playlistService.DeletePlaylistAsync(playlist.Id);
        Playlists.Remove(playlist);
    }

    /// <summary>
    /// Imports a playlist from an M3U8 file chosen by the user and adds it to the collection.
    /// </summary>
    [RelayCommand]
    private async Task ImportPlaylistAsync()
    {
        StorageFile? file = await _filesService.PickFileAsync(".m3u8", ".m3u");
        if (file == null) return;

        IReadOnlyList<string> paths = await _playlistService.ImportFromM3u8Async(file);

        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        string playlistName = Path.GetFileNameWithoutExtension(file.Name);
        playlist.Name = string.IsNullOrWhiteSpace(playlistName) ? file.Name : playlistName;

        var mediaItems = paths
            .Select(p => TryParseUri(p, file, out Uri? uri) ? uri : null)
            .Where(uri => uri != null)
            .Select(uri => _mediaFactory.GetSingleton(uri!))
            .ToList();

        if (mediaItems.Count > 0)
            await playlist.AddItemsAsync(mediaItems);
        else
            await playlist.SaveAsync();

        Playlists.Insert(0, playlist);
    }

    /// <summary>
    /// Exports the given playlist to an M3U8 file chosen by the user.
    /// </summary>
    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private async Task ExportPlaylistAsync(PlaylistViewModel playlist)
    {
        StorageFile? file = await _filesService.PickSaveFileAsync(
            playlist.Name,
            "M3U Playlist",
            new List<string> { ".m3u8" });
        if (file == null) return;

        await _playlistService.ExportToM3u8Async(playlist.Items, file);
    }

    private static bool NotEmpty(PlaylistViewModel? playlist) => playlist?.ItemsCount > 0;

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private async Task Play(PlaylistViewModel playlistVm)
    {
        var playlist = playlistVm.ToPlaylist();
        Messenger.Send(new QueuePlaylistMessage(playlist, true));
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private async Task PlayNext(PlaylistViewModel playlistVm)
    {
        Messenger.SendPlayNext(playlistVm.Items);
    }

    [RelayCommand(CanExecute = nameof(NotEmpty))]
    private async Task AddToQueue(PlaylistViewModel playlistVm)
    {
        Messenger.SendAddToQueue(playlistVm.Items);
    }

    /// <summary>
    /// Tries to parse the given path as an absolute URI.
    /// Handles local Windows/Unix file paths as well as standard URI strings.
    /// Relative paths are resolved against the directory of <paramref name="m3u8File"/>.
    /// </summary>
    private static bool TryParseUri(string path, StorageFile m3u8File, out Uri? uri)
    {
        // Try as absolute URI first (covers http://, file:///, etc.)
        if (Uri.TryCreate(path, UriKind.Absolute, out uri))
            return true;

        // Try as a local absolute file path (e.g. C:\... or /home/...)
        try
        {
            uri = new Uri(path);
            return uri.IsAbsoluteUri;
        }
        catch { }

        // Try as a path relative to the M3U8 file's directory
        try
        {
            string? folder = Path.GetDirectoryName(m3u8File.Path);
            if (!string.IsNullOrEmpty(folder))
            {
                string combined = Path.GetFullPath(Path.Combine(folder, path));
                uri = new Uri(combined);
                return true;
            }
        }
        catch { }

        uri = null;
        return false;
    }
}
