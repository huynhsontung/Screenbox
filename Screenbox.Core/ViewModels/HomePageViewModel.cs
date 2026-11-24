using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class HomePageViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<IReadOnlyList<IRecentItem>>>
{
    public ObservableCollection<IRecentItem> Recent { get; } = [];

    public SelectionViewModel Selection { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContextAlbum))]
    [NotifyPropertyChangedFor(nameof(ContextArtist))]
    [NotifyPropertyChangedFor(nameof(ContextPlaylist))]
    public partial IRecentItem? ContextMedia { get; set; }

    public AlbumViewModel? ContextAlbum => ContextMedia switch
    {
        AlbumViewModel { HasSongs: true } album => album,
        RecentMediaItemViewModel mediaItem => mediaItem.Media.Album,
        _ => null
    };

    public ArtistViewModel? ContextArtist => ContextMedia switch
    {
        ArtistViewModel { HasSongs: true } artist => artist,
        AlbumViewModel album => album.RelatedSongs.FirstOrDefault()?.MainArtist,
        RecentMediaItemViewModel mediaItem => mediaItem.Media.MainArtist,
        _ => null
    };

    public PlaylistViewModel? ContextPlaylist => ContextMedia is PlaylistViewModel { HasItems: true } playlist
        ? playlist
        : null;

    private readonly RecentContext _recentContext;
    private readonly IRecentMediaCoordinator _recentMediaCoordinator;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IQueueService _queueService;
    private readonly DispatcherQueue _dispatcherQueue;

    public HomePageViewModel(
        RecentContext recentContext,
        IRecentMediaCoordinator recentMediaCoordinator,
        SelectionViewModel selection,
        MediaViewModelFactory mediaFactory,
        IFilesService filesService,
        GeneralSettingsContext generalSettings,
        IQueueService queueService,
        IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
        Selection = selection;
        _recentContext = recentContext;
        _recentMediaCoordinator = recentMediaCoordinator;
        _mediaFactory = mediaFactory;
        _filesService = filesService;
        _generalSettings = generalSettings;
        _queueService = queueService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Selection.SetItemsSource(Recent);
        Selection.PropertyChanged += Selection_OnPropertyChanged;
        if (_generalSettings.ShowRecent)
        {
            Recent.SyncItems(_recentContext.Recent);
        }

        Messenger.Register<PropertyChangedMessage<IReadOnlyList<IRecentItem>>>(this);
    }

    public void Receive(PropertyChangedMessage<IReadOnlyList<IRecentItem>> message)
    {
        if (ReferenceEquals(message.Sender, _recentContext) && _generalSettings.ShowRecent)
        {
            _dispatcherQueue.TryExecuteOrEnqueue(() => Recent.SyncItems(message.NewValue));
        }
    }

    public async void OnLoaded()
    {
        if (_generalSettings.ShowRecent)
        {
            Recent.SyncItems(_recentContext.Recent);
            await _recentMediaCoordinator.LoadAsync();
            await _recentMediaCoordinator.LoadDetailsAsync();
        }
        else
        {
            Recent.Clear();
        }
    }

    private void Selection_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectionViewModel.IsAllSelected))
        {
            PlaySelectedCommand.NotifyCanExecuteChanged();
            PlaySelectedNextCommand.NotifyCanExecuteChanged();
            AddSelectedToQueueCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void OpenUrl(Uri? url)
    {
        if (url == null) return;
        Messenger.Send(new PlayMediaMessage(_mediaFactory.Create(url)));
    }

    [RelayCommand(CanExecute = nameof(CanPlayItem))]
    private async Task PlayAsync(IRecentItem? item)
    {
        if (!CanPlayItem(item)) return;

        switch (item)
        {
            case RecentMediaItemViewModel mediaItem:
                if (mediaItem.IsMediaActive)
                {
                    Messenger.Send(new TogglePlayPauseMessage(false));
                }
                else
                {
                    Messenger.Send(new PlayMediaMessage(mediaItem.Media, false));
                }
                break;

            case AlbumViewModel album:
                album.PlayAlbumCommand.Execute(null);
                break;

            case ArtistViewModel artist:
                artist.PlayArtistCommand.Execute(null);
                break;

            case PlaylistViewModel playlist:
                playlist.PlayCommand.Execute(null);
                break;

            case MediaCollectionViewModel collection:
                await collection.PlayAsync();
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlayItem))]
    private async Task PlayNextAsync(IRecentItem? item)
    {
        if (item is null) return;
        var items = await ResolveQueueItemsAsync(item);
        if (items.Count > 0)
        {
            Messenger.SendPlayNext(items);
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlayItem))]
    private async Task AddToQueueAsync(IRecentItem? item)
    {
        if (item is null) return;
        var items = await ResolveQueueItemsAsync(item);
        if (items.Count > 0)
        {
            Messenger.SendAddToQueue(items);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(IRecentItem? item)
    {
        if (item is null) return;
        await _recentMediaCoordinator.RemoveAsync(item);
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        StorageFolder? folder = await _filesService.PickFolderAsync();
        if (folder == null) return;
        await Messenger.PlayFilesAsync(new IStorageItem[] { folder }, _queueService, _generalSettings.EnqueueAllFilesInFolder);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task PlaySelectedAsync()
    {
        var selected = Selection.GetSelectedItems<IRecentItem>();
        Selection.DisableSelectionMode();

        if (selected.Count == 1)
        {
            await PlayAsync(selected[0]);
        }
        else if (selected.Count > 1)
        {
            var items = await ResolveSelectedQueueItemsAsync(selected);
            if (items.Length > 0)
            {
                Messenger.Send(new SetQueueMessage(new QueueSession(0, items), shouldPlay: true));
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task PlaySelectedNextAsync()
    {
        var selected = Selection.GetSelectedItems<IRecentItem>();
        Selection.DisableSelectionMode();

        var items = await ResolveSelectedQueueItemsAsync(selected);
        if (items.Length > 0)
        {
            Messenger.SendPlayNext(items);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task AddSelectedToQueueAsync()
    {
        var selected = Selection.GetSelectedItems<IRecentItem>();
        Selection.DisableSelectionMode();

        var items = await ResolveSelectedQueueItemsAsync(selected);
        if (items.Length > 0)
        {
            Messenger.SendAddToQueue(items);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task RemoveSelectedAsync()
    {
        var copy = Selection.GetSelectedItems<IRecentItem>().ToArray();
        foreach (var item in copy)
        {
            await _recentMediaCoordinator.RemoveAsync(item);
        }

        Selection.DisableSelectionMode();
    }

    private static async Task<QueueItemViewModel[]> ResolveSelectedQueueItemsAsync(IReadOnlyList<IRecentItem> selected)
    {
        var resolvedLists = await Task.WhenAll(selected.Select(ResolveQueueItemsAsync));
        return resolvedLists.SelectMany(list => list).ToArray();
    }

    private static async Task<IReadOnlyList<QueueItemViewModel>> ResolveQueueItemsAsync(IRecentItem item)
    {
        switch (item)
        {
            case RecentMediaItemViewModel mediaItem:
                return new[] { new QueueItemViewModel(mediaItem.Media) };
            case AlbumViewModel album:
            {
                QueueOrigin origin = QueueOrigin.ForAlbum(album);
                return album.OrderedSongs.Select(song => new QueueItemViewModel(song, origin)).ToArray();
            }
            case ArtistViewModel artist:
            {
                QueueOrigin origin = QueueOrigin.ForArtist(artist);
                return artist.OrderedSongs.Select(song => new QueueItemViewModel(song, origin)).ToArray();
            }
            case PlaylistViewModel playlist:
            {
                QueueOrigin origin = QueueOrigin.ForPlaylist(playlist);
                return playlist.Items.Select(entry => new QueueItemViewModel(entry.Media, origin)).ToArray();
            }
            case MediaCollectionViewModel collection:
            {
                QueueOrigin origin = QueueOrigin.ForMediaCollection(collection.Name, collection.SourcePath, collection.Token);
                var playable = await collection.GetPlayableItemsAsync();
                return playable.Select(media => new QueueItemViewModel(media, origin)).ToArray();
            }
            default:
                return Array.Empty<QueueItemViewModel>();
        }
    }

    private static bool CanPlayItem(IRecentItem? item) => item switch
    {
        AlbumViewModel album => album.HasSongs,
        ArtistViewModel artist => artist.HasSongs,
        PlaylistViewModel playlist => playlist.HasItems,
        not null => true,
        _ => false
    };

    private bool HasSelection() => Selection.SelectedRanges.Count > 0;
}
