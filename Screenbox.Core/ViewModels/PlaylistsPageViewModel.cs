#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public SelectionViewModel Selection { get; }

    private readonly IFilesService _filesService;
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;
    private readonly IPlaylistViewModelFactory _playlistFactory;

    public ObservableCollection<PlaylistViewModel> Playlists => _playlistsContext.Playlists;

    [ObservableProperty] private PlaylistViewModel? _selectedPlaylist;

    public PlaylistsPageViewModel(IFilesService filesService, IPlaylistService playlistService,
        PlaylistsContext playlistsContext, IPlaylistViewModelFactory playlistFactory,
        SelectionViewModel selection)
    {
        Selection = selection;
        _filesService = filesService;
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;
        _playlistFactory = playlistFactory;

        Selection.SetItemsSource(Playlists);
        Selection.SelectedItems.CollectionChanged += Selection_SelectedItemsChanged;
    }

    private void Selection_SelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        PlaySelectedCommand.NotifyCanExecuteChanged();
        PlaySelectedNextCommand.NotifyCanExecuteChanged();
        AddSelectedToQueueCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    public async Task CreatePlaylistAsync(string displayName)
    {
        // Create view model and add to collection
        var playlist = _playlistFactory.Create();
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
        Messenger.Send(new SetQueueMessage(playlist, true));
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

        var playlist = _playlistFactory.Create();
        playlist.Name = file.DisplayName;
        await playlist.AddItemsAsync(items);
        Playlists.Insert(0, playlist);
        Messenger.Send(new PlaylistCreatedNotificationMessage(playlist.Name));
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
    private void PlaySelected(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems
            .OfType<PlaylistViewModel>()
            .SelectMany(p => p.Items)
            .ToArray();
        Messenger.SendQueueAndPlay(items[0], items);
        selectedItems.Clear();
        Selection.ClearSelectionCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems.OfType<PlaylistViewModel>().Reverse().ToArray();
        foreach (var item in items)
        {
            Messenger.SendPlayNext(item.Items);
        }

        selectedItems.Clear();
        Selection.ClearSelectionCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddSelectedToQueue(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems.OfType<PlaylistViewModel>().ToArray();
        foreach (var item in items)
        {
            Messenger.SendAddToQueue(item.Items);
        }

        selectedItems.Clear();
        Selection.ClearSelectionCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var copy = selectedItems.OfType<PlaylistViewModel>().ToArray();
        foreach (var item in copy)
        {
            _ = DeletePlaylistAsync(item);
        }

        selectedItems.Clear();
        Selection.ClearSelectionCommand.Execute(null);
    }

    private bool HasSelection => Selection.SelectedItems.Count > 0;
}
