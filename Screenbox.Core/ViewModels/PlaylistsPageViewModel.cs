#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableRecipient
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

    /// <summary>Imports a playlist from the given M3U8 file and adds it to the collection.</summary>
    public async Task ImportPlaylistAsync(StorageFile file)
    {
        PersistentPlaylist? imported = await _playlistService.ImportPlaylistAsync(file);
        if (imported == null) return;

        await _playlistService.SavePlaylistAsync(imported);

        var playlistVm = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlistVm.Load(imported);
        Playlists.Insert(0, playlistVm);
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
}
