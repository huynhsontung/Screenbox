#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
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

    public void CreatePlaylist(string displayName)
    {
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.DisplayName = displayName;

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
    }
}
