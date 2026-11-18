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

    private readonly List<IRecentItem> _items = new();
    private readonly Dictionary<IRecentItem, RecentEntryMetadata> _metadata = new();
    private readonly Dictionary<string, RecentEntryDto> _pendingPlaylists = new(StringComparer.Ordinal);

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
        try
        {
            RelinkLibraryItems();
            await LoadAlbumArtForLinkedAlbumsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recent items after music library changed.");
        }
    }

    /// <inheritdoc/>
    public Task LoadAsync()
    {
        if (!_generalSettings.ShowRecent)
        {
            InvalidatePendingLoads();
            ClearRecentCollection();
            _isLoaded = false;
            PublishRecentItems();
            return Task.CompletedTask;
        }

        if (_isLoaded)
        {
            return Task.CompletedTask;
        }

        if (_loadTask is { IsCompleted: false })
        {
            return _loadTask;
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

            var hydrated = new List<(IRecentItem Item, RecentEntryMetadata Metadata)>(entries.Count);
            var pendingPlaylists = new List<RecentEntryDto>();

            foreach (RecentEntryDto entry in entries)
            {
                if (ct.IsCancellationRequested || !_generalSettings.ShowRecent)
                {
                    return;
                }

                if (entry.Kind == RecentItemKind.Playlist)
                {
                    var playlist = _playlistsContext.Playlists.FirstOrDefault(p => p.Id == entry.SourceId);
                    if (playlist is not null)
                    {
                        hydrated.Add((playlist, ToMetadata(entry)));
                    }
                    else
                    {
                        pendingPlaylists.Add(entry);
                    }

                    continue;
                }

                IRecentItem? vm = await HydrateRecentItemAsync(entry);
                if (ct.IsCancellationRequested || !_generalSettings.ShowRecent)
                {
                    return;
                }

                if (vm is not null)
                {
                    hydrated.Add((vm, ToMetadata(entry)));
                }
            }

            ClearRecentCollection();
            foreach (var (item, meta) in hydrated)
            {
                if (_metadata.ContainsKey(item))
                {
                    continue;
                }

                _metadata[item] = meta;
                _items.Add(item);
            }

            foreach (RecentEntryDto pending in pendingPlaylists)
            {
                _pendingPlaylists[pending.SourceId] = pending;
            }

            _isLoaded = true;
            PublishRecentItems();
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

        List<IRecentItem> snapshot = _items.ToList();
        var tasks = new List<Task>();
        foreach (IRecentItem item in snapshot)
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
        if (_items.Remove(item))
        {
            PublishRecentItems();
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
        PublishRecentItems();

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

        if (replaceEntryId is not null)
        {
            var upgradedPair = _metadata.FirstOrDefault(kvp => kvp.Value.EntryId == replaceEntryId);
            if (upgradedPair.Key is { } upgradedTarget && upgradedPair.Value.ContentHash != contentHash)
            {
                _metadata.Remove(upgradedTarget);
                _items.Remove(upgradedTarget);
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

            int existingIndex = _items.IndexOf(existing);
            if (existingIndex > 0)
            {
                _items.RemoveAt(existingIndex);
                _items.Insert(0, existing);
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
            _items.Insert(0, targetVm);

            if (origin.Kind == RecentItemKind.Playlist)
            {
                _pendingPlaylists.Remove(origin.SourceId);
            }

            while (_items.Count > RecentContext.MaxRecentItems)
            {
                IRecentItem tail = _items[^1];
                _metadata.Remove(tail);
                _items.RemoveAt(_items.Count - 1);
            }
        }

        _lastRecordedEntryId = entryId;
        PublishRecentItems();

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

    private async Task<IRecentItem?> HydrateRecentItemAsync(RecentEntryDto entry)
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
                return string.IsNullOrEmpty(entry.SourceId)
                    ? null
                    : new MediaCollectionViewModel(entry.Title, entry.SourceId, _queueService, entry.Token, Messenger);

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

    private void RelinkLibraryItems()
    {
        bool changed = false;
        for (int i = 0; i < _items.Count; i++)
        {
            IRecentItem current = _items[i];
            if (!_metadata.TryGetValue(current, out RecentEntryMetadata? meta))
            {
                continue;
            }

            IRecentItem? liveItem = null;
            if (meta.Kind == RecentItemKind.Album &&
                _libraryContext.Music.Albums.TryGetValue(meta.SourceId, out AlbumViewModel? liveAlbum))
            {
                liveAlbum.UpdateProperties();
                liveItem = liveAlbum;
            }
            else if (meta.Kind == RecentItemKind.Artist &&
                     _libraryContext.Music.Artists.TryGetValue(meta.SourceId, out ArtistViewModel? liveArtist))
            {
                liveItem = liveArtist;
            }

            if (liveItem is not null && !ReferenceEquals(current, liveItem))
            {
                _metadata.Remove(current);
                if (_metadata.ContainsKey(liveItem))
                {
                    _items.RemoveAt(i);
                    i--;
                }
                else
                {
                    _metadata[liveItem] = meta;
                    _items[i] = liveItem;
                }

                changed = true;
            }
        }

        if (changed)
        {
            PublishRecentItems();
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
                    _pendingPlaylists.Remove(removedPlaylist.Id);
                    if (_metadata.ContainsKey(removedPlaylist))
                    {
                        await RemoveAsync(removedPlaylist);
                    }
                }
            }

            if (_pendingPlaylists.Count > 0 && e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset)
            {
                ResolvePendingPlaylists();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recent items after playlists changed.");
        }
    }

    private void ResolvePendingPlaylists()
    {
        bool changed = false;
        foreach (PlaylistViewModel playlist in _playlistsContext.Playlists)
        {
            if (!_pendingPlaylists.TryGetValue(playlist.Id, out RecentEntryDto? entry))
            {
                continue;
            }

            _pendingPlaylists.Remove(playlist.Id);
            if (_metadata.ContainsKey(playlist))
            {
                continue;
            }

            RecentEntryMetadata meta = ToMetadata(entry);
            int insertIndex = FindSortedInsertIndex(meta.LastPlayed);
            if (insertIndex < RecentContext.MaxRecentItems)
            {
                _metadata[playlist] = meta;
                _items.Insert(insertIndex, playlist);
                changed = true;

                while (_items.Count > RecentContext.MaxRecentItems)
                {
                    IRecentItem tail = _items[^1];
                    _metadata.Remove(tail);
                    _items.RemoveAt(_items.Count - 1);
                }
            }
        }

        if (changed)
        {
            PublishRecentItems();
        }
    }

    private int FindSortedInsertIndex(DateTimeOffset lastPlayed)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_metadata.TryGetValue(_items[i], out RecentEntryMetadata? existingMeta) &&
                existingMeta.LastPlayed < lastPlayed)
            {
                return i;
            }
        }

        return _items.Count;
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
                PublishRecentItems();
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
        _pendingPlaylists.Clear();
        _items.Clear();
        _lastRecordedEntryId = null;
    }

    private void PublishRecentItems() =>
        _recentContext.Recent = _items.Count == 0
            ? Array.Empty<IRecentItem>()
            : _items.ToArray();

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
        var tasks = _items
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
