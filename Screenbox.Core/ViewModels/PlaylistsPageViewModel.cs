using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableObject
{
	private readonly PlaylistService _playlistService;

	[ObservableProperty]
	private ObservableCollection<PersistentPlaylist> _playlists = new();

	[ObservableProperty]
	private PersistentPlaylist? _selectedPlaylist;

	public PlaylistsPageViewModel(PlaylistService playlistService)
	{
		_playlistService = playlistService;
	}

	[RelayCommand]
	public async Task LoadPlaylistsAsync()
	{
		var loaded = await _playlistService.ListPlaylistsAsync();
		Playlists.Clear();
		foreach (var p in loaded)
			Playlists.Add(p);
	}

	[RelayCommand]
	public async Task CreatePlaylistAsync()
	{
		// UI modal should collect display name and items, then call this command
		var playlist = new PersistentPlaylist
		{
			Id = System.Guid.NewGuid().ToString(),
			DisplayName = string.Empty, // To be set by modal
			Created = System.DateTimeOffset.Now,
			Items = new()
		};
		await _playlistService.SavePlaylistAsync(playlist);
		Playlists.Add(playlist);
		SelectedPlaylist = playlist;
	}


	[RelayCommand]
	public async Task RenamePlaylistAsync(PersistentPlaylist playlist, string newName)
	{
		playlist.DisplayName = newName;
		await _playlistService.SavePlaylistAsync(playlist);
	}

	[RelayCommand]
	public async Task DeletePlaylistAsync(PersistentPlaylist playlist)
	{
		await _playlistService.DeletePlaylistAsync(playlist.Id);
		Playlists.Remove(playlist);
		if (SelectedPlaylist == playlist)
			SelectedPlaylist = null;
	}

	[RelayCommand]
	public void SelectPlaylist(PersistentPlaylist playlist)
	{
		SelectedPlaylist = playlist;
	}
}
