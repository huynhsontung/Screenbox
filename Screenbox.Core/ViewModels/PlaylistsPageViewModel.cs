#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableRecipient
{
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IPlaylistService playlistService, PlaylistsContext playlistsContext,
        INotificationService notificationService, IResourceService resourceService)
    {
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;
        _notificationService = notificationService;
        _resourceService = resourceService;
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = displayName;
        await playlist.SaveAsync();

        // Assume sort by last updated
        Playlists.Insert(0, playlist);

        _notificationService.RaiseInfo(_resourceService.GetString(ResourceName.PlaylistCreatedNotificationTitle), displayName);
    }

    public async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newDisplayName)
    {
        await playlist.RenameAsync(newDisplayName);
        _notificationService.RaiseInfo(_resourceService.GetString(ResourceName.PlaylistRenamedNotificationTitle), newDisplayName);
    }

    public async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        string name = playlist.Name;
        await _playlistService.DeletePlaylistAsync(playlist.Id);
        Playlists.Remove(playlist);
        _notificationService.RaiseInfo(_resourceService.GetString(ResourceName.PlaylistDeletedNotificationTitle), name);
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
