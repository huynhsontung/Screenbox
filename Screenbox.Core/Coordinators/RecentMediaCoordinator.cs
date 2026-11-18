using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Singleton coordinator that manages the Recent Media list, records played origins,
/// swaps deferred library/playlist entities when loaded, and persists entries to SQLite.
/// </summary>
public sealed partial class RecentMediaCoordinator : ObservableRecipient, IRecentMediaCoordinator,
    IRecipient<QueueSessionStartedMessage>,
    IRecipient<PropertyChangedMessage<MusicLibrary>>
{
    private sealed record RecentEntryMetadata(
        string EntryId,
        string ContentHash,
        RecentItemKind Kind,
        string SourceId,
        DateTimeOffset LastPlayed);

    private readonly RecentContext _recentContext;
    private readonly LibraryContext _libraryContext;
    private readonly PlaylistsContext _playlistsContext;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IDatabaseService _databaseService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly ISourceMetadataService _sourceMetadataService;
    private readonly IQueueService _queueService;
    private readonly ILogger<RecentMediaCoordinator> _logger;

    private readonly Dictionary<IRecentItem, RecentEntryMetadata> _metadata = new();

    private Task? _loadTask;
    private CancellationTokenSource? _loadCts;
    private bool _isLoaded;
    private string? _lastRecordedEntryId;

    public RecentMediaCoordinator(
        RecentContext recentContext,
        LibraryContext libraryContext,
        PlaylistsContext playlistsContext,
        GeneralSettingsContext generalSettings,
        IDatabaseService databaseService,
        MediaViewModelFactory mediaFactory,
        ISourceMetadataService sourceMetadataService,
        IQueueService queueService,
        ILogger<RecentMediaCoordinator> logger,
        IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
        _recentContext = recentContext;
        _libraryContext = libraryContext;
        _playlistsContext = playlistsContext;
        _generalSettings = generalSettings;
        _databaseService = databaseService;
        _mediaFactory = mediaFactory;
        _sourceMetadataService = sourceMetadataService;
        _queueService = queueService;
        _logger = logger;

        _playlistsContext.Playlists.CollectionChanged += Playlists_CollectionChanged;
        _generalSettings.PropertyChanged += GeneralSettings_PropertyChanged;

        Messenger.Register<QueueSessionStartedMessage>(this);
        Messenger.Register<PropertyChangedMessage<MusicLibrary>>(this);
    }

    /// <inheritdoc/>
    public async void Receive(QueueSessionStartedMessage message)
    {
        try
        {
            await HandleQueueSessionStartedAsync(message.Queue, message.IsUpgrade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle queue session started in recent coordinator.");
        }
    }

    /// <inheritdoc/>
    public async void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        if (!_generalSettings.ShowRecent || _libraryContext.IsLoadingMusic || !_isLoaded)
        {
            return;
        }

        try
        {
            await LoadAsync(forceReload: true);
            await LoadAlbumArtForLinkedAlbumsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recent items after music library changed.");
        }
    }

    /// <inheritdoc/>
    public Task LoadAsync(bool forceReload = false)
    {
        if (!_generalSettings.ShowRecent)
        {
            InvalidatePendingLoads();
            ClearRecentCollection();
            _isLoaded = false;
            PublishRecentItems(Array.Empty<IRecentItem>());
            return Task.CompletedTask;
        }

        if (!forceReload && _isLoaded)
        {
            return Task.CompletedTask;
        }

        if (!forceReload && _loadTask is { IsCompleted: false })
        {
            return _loadTask;
        }

        if (forceReload)
        {
            InvalidatePendingLoads();
        }

        return _loadTask = LoadInternalAsync();
    }

    private async Task LoadInternalAsync()
    {
        using CancellationTokenSource cts = new();
        _loadCts = cts;
        CancellationToken ct = cts.Token;
        try
        {
            var entries = await _databaseService.LoadRecentEntriesAsync(RecentContext.MaxRecentItems);
            if (ct.IsCancellationRequested || !_generalSettings.ShowRecent)
            {
                return;
            }

            var existingCollections = new Dictionary<string, MediaCollectionViewModel>(StringComparer.Ordinal);
            foreach (MediaCollectionViewModel collection in _recentContext.Recent.OfType<MediaCollectionViewModel>())
            {
                existingCollections.TryAdd(QueueOrigin.NormalizeLocation(collection.SourcePath), collection);
            }

            var hydrated = new List<(IRecentItem Item, RecentEntryMetadata Metadata)>(entries.Count);
            foreach (RecentEntryDto entry in entries)
            {
                if (ct.IsCancellationRequested || !_generalSettings.ShowRecent)
                {
                    return;
                }

                IRecentItem? vm = await HydrateRecentItemAsync(entry, existingCollections);
                if (ct.IsCancellationRequested || !_generalSettings.ShowRecent)
                {
                    return;
                }

                if (vm is not null)
                {
                    hydrated.Add((vm, ToMetadata(entry)));
                }
            }

            var items = new List<IRecentItem>(hydrated.Count);
            ClearRecentCollection();
            foreach (var (item, meta) in hydrated)
            {
                if (_metadata.ContainsKey(item))
                {
                    continue;
                }

                _metadata[item] = meta;
                items.Add(item);
            }

            _isLoaded = true;
            PublishRecentItems(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent media entries from SQLite.");
        }
        finally
        {
            if (ReferenceEquals(_loadCts, cts))
            {
                _loadCts = null;
                _loadTask = null;
            }
        }
    }

    /// <inheritdoc/>
    public async Task LoadDetailsAndThumbnailsAsync()
    {
        if (!_generalSettings.ShowRecent) return;
        await LoadAsync();
        if (!_generalSettings.ShowRecent) return;

        var tasks = new List<Task>();
        foreach (IRecentItem item in _recentContext.Recent)
        {
            if (item is MediaViewModel media)
            {
                if (!media.DetailsLoaded)
                {
                    tasks.Add(SafeLoadDetailsAsync(media));
                }

                if (media.Thumbnail is null)
                {
                    tasks.Add(SafeLoadThumbnailAsync(media));
                }
            }
            else if (item is AlbumViewModel { AlbumArt: null } album)
            {
                tasks.Add(album.LoadAlbumArtAsync());
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(IRecentItem item)
    {
        string? entryId = _metadata.TryGetValue(item, out var meta) ? meta.EntryId : null;
        _metadata.Remove(item);
        var items = _recentContext.Recent.ToList();
        if (items.Remove(item))
        {
            PublishRecentItems(items);
        }

        if (string.IsNullOrEmpty(entryId))
        {
            return;
        }

        try
        {
            await _databaseService.DeleteRecentEntryAsync(entryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recent entry '{EntryId}'.", entryId);
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        InvalidatePendingLoads();
        ClearRecentCollection();
        _isLoaded = true;
        PublishRecentItems(Array.Empty<IRecentItem>());

        try
        {
            await _databaseService.ClearRecentEntriesAsync();
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Clear();
            }
            catch
            {
                // Access cache may be unavailable in headless unit tests.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear recent media history.");
        }
    }

    public async Task HandleQueueSessionStartedAsync(QueueSession queue, bool isUpgrade)
    {
        if (!_generalSettings.ShowRecent || queue.IsEmpty)
            return;

        await LoadAsync();
        if (!_generalSettings.ShowRecent)
            return;

        MediaViewModel? fallbackItem = queue.CurrentItem ?? queue.Items.FirstOrDefault();
        if (fallbackItem is null)
            return;

        QueueOrigin origin = queue.Origin is { TrackItemsIndividually: false } containerOrigin
            ? containerOrigin
            : QueueOrigin.ForSingleMedia(fallbackItem);

        string sourceKey = !string.IsNullOrEmpty(origin.SourceId) ? origin.SourceId : origin.Title;
        string contentHash = QueueOrigin.ComputeContentHash(origin.Kind, sourceKey);
        var now = DateTimeOffset.UtcNow;
        string? replaceEntryId = isUpgrade ? _lastRecordedEntryId : null;
        var items = _recentContext.Recent.ToList();

        if (replaceEntryId is not null)
        {
            var upgradedPair = _metadata.FirstOrDefault(kvp => kvp.Value.EntryId == replaceEntryId);
            if (upgradedPair.Key is { } upgradedTarget && upgradedPair.Value.ContentHash != contentHash)
            {
                _metadata.Remove(upgradedTarget);
                items.Remove(upgradedTarget);
            }
        }

        string entryId = Guid.NewGuid().ToString();
        IRecentItem? targetVm = null;

        var existingPair = _metadata.FirstOrDefault(kvp => kvp.Value.ContentHash == contentHash);
        if (existingPair.Key is { } existing)
        {
            entryId = existingPair.Value.EntryId;
            if (existing is MediaCollectionViewModel collection && !string.IsNullOrEmpty(origin.Token))
            {
                collection.Token = origin.Token;
            }

            _metadata[existing] = existingPair.Value with { LastPlayed = now };

            int existingIndex = items.IndexOf(existing);
            if (existingIndex > 0)
            {
                items.RemoveAt(existingIndex);
                items.Insert(0, existing);
            }

            targetVm = existing;
        }
        else
        {
            targetVm = ResolveLiveItemForOrigin(origin, fallbackItem);
            if (targetVm is null)
            {
                return;
            }

            var meta = new RecentEntryMetadata(entryId, contentHash, origin.Kind, origin.SourceId, now);
            _metadata[targetVm] = meta;
            items.Insert(0, targetVm);

            while (items.Count > RecentContext.MaxRecentItems)
            {
                IRecentItem tail = items[^1];
                _metadata.Remove(tail);
                items.RemoveAt(items.Count - 1);
            }
        }

        _lastRecordedEntryId = entryId;
        PublishRecentItems(items);

        try
        {
            List<RawMediaRecordDto> dtoItems = origin.Kind == RecentItemKind.SingleMedia && targetVm is MediaViewModel mediaVm
                ? [ToRawMediaRecord(mediaVm)]
                : [];

            var dto = new RecentEntryDto
            {
                Id = entryId,
                Kind = origin.Kind,
                ContentHash = contentHash,
                Title = origin.Title,
                Subtitle = origin.Subtitle,
                SourceId = origin.SourceId,
                Token = origin.Token,
                LastPlayed = now,
                Items = dtoItems
            };

            await _databaseService.UpsertRecentEntryAsync(dto, replaceEntryId, RecentContext.MaxRecentItems);

            if (targetVm is MediaViewModel singleMedia)
            {
                if (!singleMedia.DetailsLoaded)
                {
                    await SafeLoadDetailsAsync(singleMedia);
                }

                if (singleMedia.Thumbnail is null)
                {
                    await SafeLoadThumbnailAsync(singleMedia);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist recent media entry '{ContentHash}'.", contentHash);
        }
    }

    private IRecentItem? ResolveLiveItemForOrigin(QueueOrigin origin, MediaViewModel fallbackSingle)
    {
        return origin.Kind switch
        {
            RecentItemKind.SingleMedia => fallbackSingle,
            RecentItemKind.Playlist => _playlistsContext.Playlists.FirstOrDefault(p => p.Id == origin.SourceId),
            RecentItemKind.Album => _libraryContext.Music.Albums.TryGetValue(origin.SourceId, out AlbumViewModel? album)
                ? album
                : new AlbumViewModel(origin.Title, origin.Subtitle ?? string.Empty, Messenger),
            RecentItemKind.Artist => _libraryContext.Music.Artists.TryGetValue(origin.SourceId, out ArtistViewModel? artist)
                ? artist
                : new ArtistViewModel(origin.Title, Messenger),
            RecentItemKind.MediaCollection => new MediaCollectionViewModel(origin.Title, origin.SourceId, _queueService, origin.Token, Messenger),
            _ => null
        };
    }

    private async Task<IRecentItem?> HydrateRecentItemAsync(
        RecentEntryDto entry,
        IReadOnlyDictionary<string, MediaCollectionViewModel> existingCollections)
    {
        switch (entry.Kind)
        {
            case RecentItemKind.SingleMedia:
            {
                var first = entry.Items.FirstOrDefault();
                string? path = first?.Path ?? entry.SourceId;
                if (string.IsNullOrEmpty(path)) return null;

                return await HydrateMediaViewModelAsync(first ?? new RawMediaRecordDto { Path = path, Title = entry.Title });
            }

            case RecentItemKind.MediaCollection:
            {
                if (string.IsNullOrEmpty(entry.SourceId))
                    return null;

                string key = QueueOrigin.NormalizeLocation(entry.SourceId);
                if (existingCollections.TryGetValue(key, out MediaCollectionViewModel? existingCollection))
                {
                    if (!string.IsNullOrEmpty(entry.Token))
                    {
                        existingCollection.Token = entry.Token;
                    }

                    return existingCollection;
                }

                return new MediaCollectionViewModel(entry.Title, entry.SourceId, _queueService, entry.Token, Messenger);
            }

            case RecentItemKind.Playlist:
                return _playlistsContext.Playlists.FirstOrDefault(p => p.Id == entry.SourceId) ??
                       new PlaylistViewModel(entry.SourceId, entry.Title, Messenger);

            case RecentItemKind.Album:
                if (_libraryContext.Music.Albums.TryGetValue(entry.SourceId, out AlbumViewModel? album))
                {
                    album.UpdateProperties();
                    return album;
                }

                return new AlbumViewModel(entry.Title, entry.Subtitle ?? string.Empty, Messenger);

            case RecentItemKind.Artist:
                return _libraryContext.Music.Artists.TryGetValue(entry.SourceId, out ArtistViewModel? artist)
                    ? artist
                    : new ArtistViewModel(entry.Title, Messenger);

            default:
                return null;
        }
    }

    private async void Playlists_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
            {
                foreach (PlaylistViewModel removedPlaylist in e.OldItems.OfType<PlaylistViewModel>())
                {
                    IRecentItem? match = _recentContext.Recent
                        .OfType<PlaylistViewModel>()
                        .FirstOrDefault(p => p.Id == removedPlaylist.Id);
                    if (match is not null)
                    {
                        await RemoveAsync(match);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recent items after playlists changed.");
        }
    }

    private async void GeneralSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettingsContext.ShowRecent))
        {
            if (_generalSettings.ShowRecent)
            {
                await LoadAsync();
                await LoadDetailsAndThumbnailsAsync();
            }
            else
            {
                InvalidatePendingLoads();
                ClearRecentCollection();
                _isLoaded = false;
                PublishRecentItems(Array.Empty<IRecentItem>());
            }
        }
    }

    private void InvalidatePendingLoads()
    {
        _loadCts?.Cancel();
        _loadCts = null;
        _loadTask = null;
    }

    private void ClearRecentCollection()
    {
        _metadata.Clear();
        _lastRecordedEntryId = null;
    }

    private void PublishRecentItems(IReadOnlyList<IRecentItem> items) =>
        _recentContext.Recent = items.Count == 0
            ? Array.Empty<IRecentItem>()
            : items as IRecentItem[] ?? items.ToArray();

    private async Task<MediaViewModel?> HydrateMediaViewModelAsync(RawMediaRecordDto record)
    {
        if (string.IsNullOrEmpty(record.Path))
            return null;

        StorageFile? file = await FilesHelpers.TryGetFileFromTokenAsync(record.Token);

        MediaViewModel media;
        bool existing;

        if (file is not null)
        {
            existing = _mediaFactory.TryGetOrCreate(file, out media);
        }
        else if (Uri.TryCreate(record.Path, UriKind.Absolute, out Uri? uri))
        {
            existing = _mediaFactory.TryGetOrCreate(uri, out media);
        }
        else
        {
            media = _mediaFactory.GetOrCreate(record.Path);
            existing = false;
        }

        if (media.Source is IFileMediaSource fileSource && !string.IsNullOrEmpty(record.Token))
        {
            fileSource.Token = record.Token;
        }

        if (!existing)
        {
            if (!string.IsNullOrEmpty(record.Title))
            {
                media.Name = record.Title;
            }

            media.MediaInfo = record.ToMediaInfo();
            if (record.DateAdded != default)
            {
                media.DateAdded = record.DateAdded.ToLocalTime();
            }
        }

        return media;
    }

    private async Task LoadAlbumArtForLinkedAlbumsAsync()
    {
        var tasks = _recentContext.Recent
            .OfType<AlbumViewModel>()
            .Where(album => album.AlbumArt is null)
            .Select(album => album.LoadAlbumArtAsync())
            .ToList();

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private static RecentEntryMetadata ToMetadata(RecentEntryDto entry) =>
        new(entry.Id, entry.ContentHash, entry.Kind, entry.SourceId, entry.LastPlayed);

    private static RawMediaRecordDto ToRawMediaRecord(MediaViewModel media)
    {
        string? token = null;
        if (media.Source is IFileMediaSource fileSource)
        {
            token = fileSource.Token;
            if (string.IsNullOrEmpty(token) && fileSource.StorageFile is { } file)
            {
                try
                {
                    token = StorageApplicationPermissions.MostRecentlyUsedList.Add(
                        file,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                    fileSource.Token = token;
                }
                catch
                {
                    // Access cache may be unavailable in headless unit tests.
                }
            }
        }

        return new RawMediaRecordDto
        {
            Path = media.Location,
            Title = media.Name,
            MediaType = media.MediaType,
            Duration = media.Duration,
            Token = token
        };
    }

    private async Task SafeLoadDetailsAsync(MediaViewModel media)
    {
        try
        {
            await media.LoadDetailsAsync(_sourceMetadataService);
        }
        catch (ArgumentException)
        {
            // Expected if underlying StorageFile is stale.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load recent media details for file type '{FileType}'.", Path.GetExtension(media.Location));
        }
    }

    private async Task SafeLoadThumbnailAsync(MediaViewModel media)
    {
        try
        {
            await media.LoadThumbnailAsync();
        }
        catch (ArgumentException)
        {
            // Expected if underlying StorageFile is stale.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load a thumbnail for file type '{FileType}'.", Path.GetExtension(media.Location));
        }
    }
}
