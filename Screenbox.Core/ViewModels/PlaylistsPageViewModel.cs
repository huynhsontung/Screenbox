#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    public async Task LoadPlaylistsAsync()
    {
        var loaded = await _playlistService.ListPlaylistsAsync();
        Playlists.Clear();
        foreach (var p in loaded)
        {
            var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
            playlist.Load(p);
            Playlists.Add(playlist);
        }
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Generate a new unique ID
        var id = Guid.NewGuid().ToString();
        
        // Create the persistent playlist
        var persistentPlaylist = new PersistentPlaylist
        {
            Id = id,
            DisplayName = displayName,
            LastUpdated = DateTimeOffset.Now,
            Items = new()
        };

        // Save to disk
        await _playlistService.SavePlaylistAsync(persistentPlaylist);

        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Load(persistentPlaylist);

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
    }
}
