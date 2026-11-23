using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
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
    private readonly RecentContext _recentContext;
    private readonly LibraryContext _libraryContext;
    private readonly PlaylistsContext _playlistsContext;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IDatabaseService _databaseService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly ISourceMetadataService _sourceMetadataService;
    private readonly IQueueService _queueService;
    private readonly ILogger<RecentMediaCoordinator> _logger;
    private readonly Func<PlaylistViewModel> _playlistFactory;

    private readonly Lock _stateLock = new();
    private Dictionary<IRecentItem, string> _entryIds = new();
    private IRecentItem[] _items = [];

    private Task? _loadTask;
    private CancellationTokenSource? _loadCts;
    private bool _isLoaded;

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
        IMessenger? messenger = null,
        Func<PlaylistViewModel>? playlistFactory = null)
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
        _playlistFactory = playlistFactory ?? Ioc.Default.GetRequiredService<PlaylistViewModel>;

        _playlistsContext.Playlists.CollectionChanged += Playlists_CollectionChanged;

        Messenger.Register<QueueSessionStartedMessage>(this);
        Messenger.Register<PropertyChangedMessage<MusicLibrary>>(this);
    }

    /// <inheritdoc/>
    public async void Receive(QueueSessionStartedMessage message)
    {
        try
        {
            await HandleQueueSessionStartedAsync(message.Origin, message.Media, message.ReplacedEntryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle queue session started in recent coordinator.");
        }
    }

    /// <inheritdoc/>
    public async void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        if (_libraryContext.IsLoadingMusic)
        {
            return;
        }

        bool isLoaded;
        lock (_stateLock)
        {
            isLoaded = _isLoaded;
            if (!_generalSettings.ShowRecent)
            {
                // Mark stale so re-enabling ShowRecent refreshes retained album/artist cards against the new library.
                _isLoaded = false;
                return;
            }
        }

        if (!isLoaded)
        {
            return;
        }

        try
        {
            // Replacing placeholders swaps in linked library items; visible containers then load
            // only the thumbnail needed for display (e.g., RelatedSongs[0] for albums) via ThumbnailGridViewBehavior.
            await LoadAsync(forceReload: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recent items after music library changed.");
        }
    }

    /// <inheritdoc/>
    public Task LoadAsync(bool forceReload = false)
    {
        lock (_stateLock)
        {
            if (!_generalSettings.ShowRecent)
            {
                return Task.CompletedTask;
            }

            if (!forceReload && _loadTask is { IsCompleted: false })
            {
                return _loadTask;
            }

            if (!forceReload && _isLoaded)
            {
                return Task.CompletedTask;
            }

            if (forceReload)
            {
                InvalidatePendingLoadsLocked();
            }

            CancellationTokenSource cts = new();
            _loadCts = cts;
            return _loadTask = LoadInternalAsync(cts);
        }
    }

    private async Task LoadInternalAsync(CancellationTokenSource cts)
    {
        CancellationToken ct = cts.Token;
        try
        {
            var entries = await _databaseService.LoadRecentEntriesAsync(RecentContext.MaxRecentItems);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            Dictionary<string, IRecentItem> existingByEntryId;
            lock (_stateLock)
            {
                existingByEntryId = new Dictionary<string, IRecentItem>(_entryIds.Count, StringComparer.Ordinal);
                foreach (var (item, id) in _entryIds)
                {
                    existingByEntryId.TryAdd(id, item);
                }
            }

            var newItems = new List<IRecentItem>(entries.Count);
            var newEntryIds = new Dictionary<IRecentItem, string>(entries.Count);
            foreach (RecentEntryDto entry in entries)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                IRecentItem? vm = await HydrateRecentItemAsync(entry, existingByEntryId);
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (vm is not null && newEntryIds.TryAdd(vm, entry.Id))
                {
                    newItems.Add(vm);
                }
            }

            lock (_stateLock)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                _entryIds = newEntryIds;
                _items = ToSnapshot(newItems);
                _isLoaded = true;
            }

            PublishSnapshot();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent media entries from SQLite.");
        }
        finally
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_loadCts, cts))
                {
                    _loadCts = null;
                    _loadTask = null;
                }
            }

            cts.Dispose();
        }
    }

    /// <inheritdoc/>
    public Task LoadDetailsAsync()
    {
        var tasks = _recentContext.Recent.OfType<RecentMediaItemViewModel>()
            .Select(item => item.Media)
            .Where(media => !media.DetailsLoaded)
            .Select(SafeLoadDetailsAsync);

        // Thumbnails (including album art loaded from AlbumViewModel.RelatedSongs[0]) are deferred
        // to ThumbnailGridViewBehavior so only visible UI containers decode thumbnails.
        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(IRecentItem item)
    {
        string? entryId;
        bool changed = false;

        lock (_stateLock)
        {
            _entryIds.Remove(item, out entryId);
            var items = _items.ToList();
            if (items.Remove(item))
            {
                _items = ToSnapshot(items);
                changed = true;
            }
        }

        if (changed)
        {
            PublishSnapshot();
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
        lock (_stateLock)
        {
            InvalidatePendingLoadsLocked();
            _entryIds = new();
            _items = [];
            _isLoaded = true;
        }

        PublishSnapshot();

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

    public async Task HandleQueueSessionStartedAsync(QueueOrigin origin, MediaViewModel? media = null, string? replacedEntryId = null)
    {
        if (!_generalSettings.ShowRecent)
            return;

        if (origin.TrackItemsIndividually)
        {
            if (media is null)
                return;

            origin = QueueOrigin.ForSingleMedia(media);
        }

        await LoadAsync();

        string entryId = origin.GetEntryId();
        var now = DateTimeOffset.UtcNow;
        string? replaceEntryId = !string.IsNullOrEmpty(replacedEntryId) &&
                                 !string.Equals(replacedEntryId, entryId, StringComparison.Ordinal)
            ? replacedEntryId
            : null;
        IRecentItem? targetVm;

        lock (_stateLock)
        {
            if (!_generalSettings.ShowRecent)
                return;

            var items = _items.ToList();

            if (replaceEntryId is not null)
            {
                var upgradedPair = _entryIds.FirstOrDefault(kvp => kvp.Value == replaceEntryId);
                if (upgradedPair.Key is { } upgradedTarget)
                {
                    _entryIds.Remove(upgradedTarget);
                    items.Remove(upgradedTarget);
                }
            }

            var existingPair = _entryIds.FirstOrDefault(kvp => kvp.Value == entryId);
            if (existingPair.Key is { } existing)
            {
                IRecentItem promoted = existing;
                if (origin.Kind == RecentItemKind.SingleMedia &&
                    existing is RecentMediaItemViewModel existingRecent &&
                    media is not null &&
                    !ReferenceEquals(existingRecent.Media, media))
                {
                    // Swap to a wrapper around the live playing instance (e.g. replacing a hydrated placeholder
                    // with a live library/file VM) so metadata, album/artist links, and thumbnails stay live.
                    var updated = new RecentMediaItemViewModel(media, Messenger);
                    _entryIds.Remove(existing);
                    _entryIds[updated] = entryId;
                    promoted = updated;
                }
                else if (existing is MediaCollectionViewModel collection && !string.IsNullOrEmpty(origin.Token))
                {
                    collection.Token = origin.Token;
                }

                int existingIndex = items.IndexOf(existing);
                if (!ReferenceEquals(existing, promoted))
                {
                    if (existingIndex >= 0)
                    {
                        items.RemoveAt(existingIndex);
                    }

                    items.Insert(0, promoted);
                }
                else if (existingIndex > 0)
                {
                    items.RemoveAt(existingIndex);
                    items.Insert(0, existing);
                }

                targetVm = promoted;
            }
            else
            {
                targetVm = ResolveLiveItemForOrigin(origin, media);
                if (targetVm is null)
                {
                    return;
                }

                _entryIds[targetVm] = entryId;
                items.Insert(0, targetVm);

                while (items.Count > RecentContext.MaxRecentItems)
                {
                    IRecentItem tail = items[^1];
                    _entryIds.Remove(tail);
                    items.RemoveAt(items.Count - 1);
                }
            }

            _items = ToSnapshot(items);
        }

        PublishSnapshot();

        try
        {
            string? token = origin.Kind == RecentItemKind.SingleMedia && targetVm is RecentMediaItemViewModel recentMedia
                ? EnsureFileAccessToken(recentMedia.Media)
                : origin.Token;

            var dto = new RecentEntryDto
            {
                Id = entryId,
                Kind = origin.Kind,
                Title = origin.Title,
                Subtitle = origin.Subtitle,
                SourceId = origin.SourceId,
                Token = token,
                LastPlayed = now
            };

            await _databaseService.UpsertRecentEntryAsync(dto, replaceEntryId, RecentContext.MaxRecentItems);

            if (targetVm is RecentMediaItemViewModel { Media.DetailsLoaded: false } singleMedia)
            {
                await SafeLoadDetailsAsync(singleMedia.Media);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist recent media entry for kind '{Kind}'.", origin.Kind);
        }
    }

    private IRecentItem? ResolveLiveItemForOrigin(QueueOrigin origin, MediaViewModel? singleMedia)
    {
        return origin.Kind switch
        {
            RecentItemKind.SingleMedia => singleMedia is not null ? new RecentMediaItemViewModel(singleMedia, Messenger) : null,
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
        IReadOnlyDictionary<string, IRecentItem> existingByEntryId)
    {
        switch (entry.Kind)
        {
            case RecentItemKind.SingleMedia:
            {
                if (existingByEntryId.TryGetValue(entry.Id, out IRecentItem? existing) &&
                    existing is RecentMediaItemViewModel existingRecent)
                {
                    if (FindMusicLibrarySong(entry.SourceId) is { } librarySong &&
                        !ReferenceEquals(existingRecent.Media, librarySong))
                    {
                        return new RecentMediaItemViewModel(librarySong, Messenger);
                    }

                    if (existingRecent.Media.Source is IFileMediaSource fileSource && !string.IsNullOrEmpty(entry.Token))
                    {
                        fileSource.Token = entry.Token;
                    }

                    return existingRecent;
                }

                return await HydrateMediaViewModelAsync(entry) is { } hydratedMedia
                    ? new RecentMediaItemViewModel(hydratedMedia, Messenger)
                    : null;
            }

            case RecentItemKind.MediaCollection:
            {
                if (string.IsNullOrEmpty(entry.SourceId))
                    return null;

                if (existingByEntryId.TryGetValue(entry.Id, out IRecentItem? existing) &&
                    existing is MediaCollectionViewModel existingCollection)
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
            {
                if (_playlistsContext.Playlists.FirstOrDefault(p => p.Id == entry.SourceId) is { } existingPlaylist)
                {
                    return existingPlaylist;
                }

                PlaylistViewModel placeholder = _playlistFactory();
                placeholder.LoadPlaceholder(entry.SourceId, entry.Title);
                return placeholder;
            }

            case RecentItemKind.Album:
                return _libraryContext.Music.Albums.TryGetValue(entry.SourceId, out AlbumViewModel? album)
                    ? album
                    : new AlbumViewModel(entry.Title, entry.Subtitle ?? string.Empty, Messenger);

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
                    IRecentItem? match = Volatile.Read(ref _items)
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

    private void InvalidatePendingLoadsLocked()
    {
        _loadCts?.Cancel();
        _loadCts = null;
        _loadTask = null;
    }

    private static IRecentItem[] ToSnapshot(List<IRecentItem> items) =>
        items.Count == 0 ? [] : [.. items];

    private void PublishSnapshot() =>
        _recentContext.Recent = Volatile.Read(ref _items);

    private MediaViewModel? FindMusicLibrarySong(string location)
    {
        string normalized = QueueOrigin.NormalizeLocation(location);
        if (string.IsNullOrEmpty(normalized))
            return null;

        // Only music library songs carry live Album/Artist links needed for Show Album / Show Artist actions;
        // video library items have no relational container entities to relink.
        return _libraryContext.Music.Songs.FirstOrDefault(song =>
            string.Equals(QueueOrigin.NormalizeLocation(song.Location), normalized, StringComparison.Ordinal));
    }

    private async Task<MediaViewModel?> HydrateMediaViewModelAsync(RecentEntryDto entry)
    {
        if (string.IsNullOrEmpty(entry.SourceId))
            return null;

        StorageFile? file = await FilesHelpers.TryGetFileFromTokenAsync(entry.Token);

        MediaViewModel media;
        bool existing;

        if (file is not null)
        {
            existing = _mediaFactory.TryGetOrCreate(file, out media);
        }
        else if (Uri.TryCreate(entry.SourceId, UriKind.Absolute, out Uri? uri))
        {
            existing = _mediaFactory.TryGetOrCreate(uri, out media);
        }
        else
        {
            media = _mediaFactory.GetOrCreate(entry.SourceId);
            existing = false;
        }

        if (media.Source is IFileMediaSource fileSource && !string.IsNullOrEmpty(entry.Token))
        {
            fileSource.Token = entry.Token;
        }

        if (!existing)
        {
            if (entry.MediaRecord is { } record)
            {
                string title = !string.IsNullOrEmpty(record.Title) ? record.Title : entry.Title;
                if (!string.IsNullOrEmpty(title))
                {
                    media.Name = title;
                }

                media.MediaInfo = record.ToMediaInfo();
                if (record.DateAdded != default)
                {
                    media.DateAdded = record.DateAdded.ToLocalTime();
                }
            }
            else if (!string.IsNullOrEmpty(entry.Title))
            {
                media.Name = entry.Title;
            }
        }

        return media;
    }

    private static string? EnsureFileAccessToken(MediaViewModel media)
    {
        if (media.Source is not IFileMediaSource fileSource)
            return null;

        string? token = fileSource.Token;
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

        return token;
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
}
