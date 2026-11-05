using Screenbox.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistsPageViewModel : ObservableRecipient
{
    public SelectionViewModel Selection { get; }

    [ObservableProperty]
    public partial PlaylistViewModel? ContextPlaylist { get; set; }

    private readonly IFilesService _filesService;
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] public partial PlaylistViewModel? SelectedPlaylist { get; set; }

    public PlaylistsPageViewModel(IFilesService filesService, IPlaylistService playlistService,
        PlaylistsContext playlistsContext, SelectionViewModel selection)
    {
        Selection = selection;
        _filesService = filesService;
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;

        Selection.SetItemsSource(Playlists);
        ((INotifyCollectionChanged)Selection.SelectedRanges).CollectionChanged += Selection_SelectedRangesChanged;
    }

    private void Selection_SelectedRangesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
        PlaySelectedNextCommand.NotifyCanExecuteChanged();
        AddSelectedToQueueCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = displayName;
        await playlist.SaveAsync();

        // Assume sort by last updated
        Playlists.Insert(0, playlist);
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistCreated, title: displayName));
    }

    public async Task RenamePlaylistAsync(PlaylistViewModel playlist, string newDisplayName)
    {
        await playlist.RenameAsync(newDisplayName);
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistRenamed, title: newDisplayName));
    }

    public async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        string playlistName = playlist.Name;
        await _playlistService.DeletePlaylistAsync(playlist.Id);
        Playlists.Remove(playlist);
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistDeleted, title: playlistName));
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
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistCreated, title: playlist.Name));
    }

    public async Task ExportPlaylistAsync(PlaylistViewModel playlist, string playlistFileDisplayName = "M3U8")
    {
        var saveFileTypes = new Dictionary<string, IList<string>> { [playlistFileDisplayName] = [".m3u8"] };
        StorageFile? file = await _filesService.PickSaveFileAsync(playlist.Name,
            saveFileTypes, Windows.Storage.Pickers.PickerLocationId.MusicLibrary);
        if (file is null) return;

        await _playlistService.ExportPlaylistItemsAsync(playlist.Items, file);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelected()
    {
        var selectedPlaylists = Selection.GetSelectedItems<PlaylistViewModel>();
        var items = selectedPlaylists
            .SelectMany(p => p.Items)
            .ToArray();
        if (items.Length > 0)
        {
            var origin = selectedPlaylists.Count == 1
                ? QueueOrigin.ForPlaylist(selectedPlaylists[0])
                : QueueOrigin.ForMediaCollection(items[0].Name);
            Messenger.SendQueueAndPlay(items[0], items, origin: origin);
        }
        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext()
    {
        var selectedItems = Selection.GetSelectedItems<PlaylistViewModel>();
        if (selectedItems.Count == 0)
            return;

        selectedItems.Reverse();
        foreach (var item in selectedItems)
        {
            Messenger.SendPlayNext(item.Items);
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddSelectedToQueue()
    {
        var selectedItems = Selection.GetSelectedItems<PlaylistViewModel>();
        foreach (var item in selectedItems)
        {
            Messenger.SendAddToQueue(item.Items);
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        var selectedItems = Selection.GetSelectedItems<PlaylistViewModel>();
        foreach (var item in selectedItems)
        {
            _ = DeletePlaylistAsync(item);
        }

        Selection.DisableSelectionMode();
    }

    private bool HasSelection => Selection.SelectedRanges.Count > 0;
}
