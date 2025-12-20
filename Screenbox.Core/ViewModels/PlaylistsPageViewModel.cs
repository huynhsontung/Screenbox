#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Contexts;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IPlaylistService playlistService, PlaylistsContext playlistsContext)
    {
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Caption = displayName;
        await playlist.SaveAsync();

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
    }

    public async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newDisplayName)
    {
        playlist.Caption = newDisplayName;
        await playlist.SaveAsync();
    }

    public async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        await _playlistService.DeletePlaylistAsync(playlist.Id);
        Playlists.Remove(playlist);
    }
}
