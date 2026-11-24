using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaylistViewModel : ObservableRecipient,
    IRecentItem,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    private const int ThumbnailDecodePixelHeight = 300;

    public ObservableCollection<PlaylistItemViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }
    [ObservableProperty] public partial BitmapImage? Thumbnail { get; set; }
    [ObservableProperty] public partial DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public string Id => _id.ToString();

    public bool HasItems => Items.Count > 0;

    private Guid _id = Guid.NewGuid();

    private bool _isLoading;
    private int _thumbnailVersion;
    private CancellationTokenSource? _thumbnailCts;
    private Task? _thumbnailLoadTask;

    private readonly IPlaylistService _playlistService;
    private readonly IPlaylistThumbnailService? _thumbnailService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _playlistSaveTimer;
    private readonly DispatcherQueueTimer _collageTimer;
    private readonly ILogger<PlaylistViewModel> _logger;

    public PlaylistViewModel(
        IPlaylistService playlistService,
        IPlaylistThumbnailService thumbnailService,
        MediaViewModelFactory mediaFactory,
        ILogger<PlaylistViewModel> logger)
        : this(playlistService, mediaFactory, logger, WeakReferenceMessenger.Default, thumbnailService)
    {
    }

    public PlaylistViewModel(
        IPlaylistService playlistService,
        MediaViewModelFactory mediaFactory,
        ILogger<PlaylistViewModel> logger,
        IMessenger messenger,
        IPlaylistThumbnailService? thumbnailService = null)
        : base(messenger)
    {
        _playlistService = playlistService;
        _thumbnailService = thumbnailService;
        _mediaFactory = mediaFactory;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _playlistSaveTimer = _dispatcherQueue.CreateTimer();
        _collageTimer = _dispatcherQueue.CreateTimer();

        Items.CollectionChanged += Items_CollectionChanged;

        Messenger.Register<QueuePlaybackStateChangedMessage>(this);
        RefreshPlaybackState();
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(HasItems));
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();

        if (_isLoading)
            return;

        _playlistSaveTimer.Debounce(async () =>
        {
            // Ensures the playlist is up to date when items are reordered.
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist playlist '{PlaylistId}'.", Id);
            }
        }, TimeSpan.FromMilliseconds(50));

        if (ShouldRefreshThumbnail(e))
        {
            _collageTimer.Debounce(async () =>
            {
                await UpdateThumbnailCollageAsync();
            }, TimeSpan.FromMilliseconds(50));
        }
    }

    private bool ShouldRefreshThumbnail(NotifyCollectionChangedEventArgs e)
    {
        if (Items.Count == 0)
            return true;

        if (Thumbnail is null && e.Action == NotifyCollectionChangedAction.Add)
            return true;

        const int cellCount = PlaylistThumbnailService.MaxArtworkCount;
        return e.Action switch
        {
            NotifyCollectionChangedAction.Add => e.NewStartingIndex < cellCount,
            NotifyCollectionChangedAction.Remove => e.OldStartingIndex < cellCount,
            NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move =>
                e.NewStartingIndex < cellCount || e.OldStartingIndex < cellCount,
            NotifyCollectionChangedAction.Reset => true,
            _ => false
        };
    }

    public QueueSession ToQueueSession()
    {
        return new QueueSession(0, Items.Select(item => item.Media).ToList(), origin: QueueOrigin.ForPlaylist(this));
    }

    /// <summary>
    /// Populates placeholder identity and display metadata without modifying <see cref="Items"/> or triggering persistence.
    /// </summary>
    public void LoadPlaceholder(string id, string displayName)
    {
        if (!Guid.TryParse(id, out _id)) return;
        Name = displayName;
        RefreshPlaybackState();
    }

    public void Load(PlaylistRecordDto persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        _isLoading = true;
        Items.Clear();

        RefreshPlaybackState();

        var albumFactory = new AlbumViewModelFactory();
        var artistFactory = new ArtistViewModelFactory();

        foreach (var item in persistentPlaylist.Items)
        {
            try
            {
                var vm = ToMediaViewModel(item, albumFactory, artistFactory);
                Items.Add(new PlaylistItemViewModel(vm));
            }
            catch { }
        }
        _isLoading = false;
    }

    /// <summary>
    /// Loads the playlist thumbnail from cache, generating and caching a collage if no saved thumbnail exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public Task LoadThumbnailAsync()
    {
        if (Thumbnail is not null || _thumbnailService is null)
            return Task.CompletedTask;

        if (_thumbnailLoadTask is { IsCompleted: false } existingTask)
            return existingTask;

        Task loadTask = LoadThumbnailCoreAsync();
        _thumbnailLoadTask = loadTask;

        return loadTask;
    }

    private async Task LoadThumbnailCoreAsync()
    {
        if (_thumbnailService is null)
            return;

        (int version, CancellationToken ct) = BeginThumbnailOperation();
        try
        {
            // Check cache even when Items is empty so recent-playlist placeholders on HomePage display cached art.
            byte[]? cachedBytes = await _thumbnailService.ReadThumbnailAsync(Id, ct);
            if (version != _thumbnailVersion || ct.IsCancellationRequested)
                return;

            if (cachedBytes is not null)
            {
                if (await TryApplyThumbnailBytesAsync(cachedBytes, version, ct))
                    return;

                // Cached file is corrupt or unreadable; remove it and fall through to regeneration.
                await _thumbnailService.DeleteThumbnailAsync(Id);
            }

            if (Items.Count == 0 || version != _thumbnailVersion || ct.IsCancellationRequested)
                return;

            await GenerateThumbnailCoreAsync(version, ct);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer update.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load thumbnail for playlist {PlaylistId}.", Id);
        }
    }

    public async Task SaveAsync()
    {
        LastUpdated = DateTimeOffset.Now;
        var persistentPlaylist = ToPersistentPlaylist();
        await _playlistService.SavePlaylistAsync(persistentPlaylist);
    }

    private void RefreshPlaybackState()
    {
        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
    }

    public async Task RenameAsync(string newDisplayName)
    {
        Name = newDisplayName;
        await SaveAsync();
    }

    public void Receive(QueuePlaybackStateChangedMessage message)
    {
        UpdatePlaybackState(message.State);
    }

    private void UpdatePlaybackState(QueuePlaybackState state)
    {
        bool isActive = state.SessionId != Guid.Empty &&
                        state.CurrentItem is not null &&
                        (state.Origin?.Matches(RecentItemKind.Playlist, Id) ?? false);
        IsQueueActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void Play()
    {
        if (IsQueueActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(showBadge: false));
            return;
        }

        Messenger.Send(new SetQueueMessage(ToQueueSession(), true));
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void PlayNext()
    {
        Messenger.SendPlayNext(Items.Select(item => item.Media).ToList(), QueueOrigin.ForPlaylist(this));
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void AddToQueue()
    {
        Messenger.SendAddToQueue(Items.Select(item => item.Media).ToList(), QueueOrigin.ForPlaylist(this));
    }

    [RelayCommand]
    public async Task AddItemsAsync(IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;
        foreach (var item in items)
        {
            Items.Add(new PlaylistItemViewModel(item));
        }

        //await SaveAsync();
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistItemsAdded, title: Name, numericValue: items.Count));
    }

    public async Task AddItemsAtIndexAsync(IReadOnlyList<MediaViewModel> items, int insertIndex = -1)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            var entry = new PlaylistItemViewModel(item);
            if (insertIndex < 0 || insertIndex >= Items.Count)
            {
                Items.Add(entry);
            }
            else
            {
                Items.Insert(insertIndex, entry);
                insertIndex++;
            }
        }

        //await SaveAsync();
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistItemsAdded, title: Name, numericValue: items.Count));
    }

    internal async Task UpdateThumbnailCollageAsync()
    {
        if (_thumbnailService is null)
            return;

        (int version, CancellationToken ct) = BeginThumbnailOperation();

        if (Items.Count == 0)
        {
            Thumbnail = null;
            await _thumbnailService.DeleteThumbnailAsync(Id);
            return;
        }

        try
        {
            await GenerateThumbnailCoreAsync(version, ct);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer update.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update thumbnail for playlist {PlaylistId}.", Id);
        }
    }

    private (int Version, CancellationToken Token) BeginThumbnailOperation()
    {
        _thumbnailCts?.Cancel();
        _thumbnailCts?.Dispose();

        CancellationTokenSource cts = new();
        _thumbnailCts = cts;
        int version = ++_thumbnailVersion;

        return (version, cts.Token);
    }

    private async Task GenerateThumbnailCoreAsync(int version, CancellationToken cancellationToken)
    {
        if (_thumbnailService is null)
            return;

        MediaViewModel[] itemsSnapshot = Items.Select(item => item.Media).ToArray();
        byte[]? encodedBytes = await _thumbnailService.BuildAndCacheThumbnailAsync(Id, itemsSnapshot, cancellationToken);
        if (version != _thumbnailVersion || cancellationToken.IsCancellationRequested)
            return;

        if (encodedBytes is null)
        {
            Thumbnail = null;
            return;
        }

        await TryApplyThumbnailBytesAsync(encodedBytes, version, cancellationToken);
    }

    private async Task<bool> TryApplyThumbnailBytesAsync(byte[] imageBytes, int version, CancellationToken cancellationToken)
    {
        try
        {
            BitmapImage bitmapImage = new()
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = ThumbnailDecodePixelHeight
            };

            using MemoryStream memoryStream = new(imageBytes);
            await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());

            if (version != _thumbnailVersion || cancellationToken.IsCancellationRequested)
                return false;

            Thumbnail = bitmapImage;
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to decode cached thumbnail for playlist {PlaylistId}.", Id);
            return false;
        }
    }

    private PlaylistRecordDto ToPersistentPlaylist()
    {
        return new PlaylistRecordDto
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(item => RawMediaRecordDto.FromMediaViewModel(item.Media)).ToList()
        };
    }

    private MediaViewModel ToMediaViewModel(RawMediaRecordDto record, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
    {
        MediaViewModel media;
        bool existing = false;
        if (Uri.TryCreate(record.Path, UriKind.Absolute, out var uri))
        {
            if (_mediaFactory.TryGetOrCreate(uri, out var existingMedia))
            {
                media = existingMedia!;
                existing = true;
            }
            else
            {
                media = _mediaFactory.GetOrCreate(uri);
            }
        }
        else
        {
            media = _mediaFactory.Create(new Uri("about:blank"));
            media.IsAvailable = false;
        }

        if (!existing)
        {
            if (!string.IsNullOrEmpty(record.Title))
                media.Name = record.Title;

            media.MediaInfo = record.ToMediaInfo();

            if (media.MediaType == MediaPlaybackType.Music)
            {
                // Placeholder album and artist for media list view.
                // This path can happen when playlist is loaded before the music library is loaded
                albumFactory.AddSong(media);
                artistFactory.AddSong(media);
                media.Album = albumFactory.SongsToAlbums[media];
                media.Artists = artistFactory.SongsToArtists[media].ToArray();
                media.Album.UpdateProperties();
            }

            if (record.DateAdded != default)
            {
                media.DateAdded = record.DateAdded.ToLocalTime();
            }
        }

        return media;
    }
}
