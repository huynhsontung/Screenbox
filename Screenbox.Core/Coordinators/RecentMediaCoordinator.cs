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
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace Screenbox.Core.Coordinators;

/// <summary>
/// Singleton coordinator that manages the Recent Media list, tracks active play queue sessions,
/// synchronizes active/playing state across recent items, links library/playlist entities, and persists to SQLite.
/// </summary>
public sealed partial class RecentMediaCoordinator : ObservableRecipient, IRecentMediaCoordinator,
    IRecipient<QueueSessionStartedMessage>,
    IRecipient<PropertyChangedMessage<MusicLibrary>>
{
    private readonly RecentContext _recentContext;
    private readonly PlayQueueContext _playQueueContext;
    private readonly LibraryContext _libraryContext;
    private readonly PlaylistsContext _playlistsContext;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IDatabaseService _databaseService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly ISourceMetadataService _sourceMetadataService;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly ILogger<RecentMediaCoordinator> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private MediaViewModel? _monitoredCurrentItem;
    private string? _lastRecordedEntryId;

    public RecentMediaCoordinator(
        RecentContext recentContext,
        PlayQueueContext playQueueContext,
        LibraryContext libraryContext,
        PlaylistsContext playlistsContext,
        GeneralSettingsContext generalSettings,
        IDatabaseService databaseService,
        MediaViewModelFactory mediaFactory,
        ISourceMetadataService sourceMetadataService,
        ILogger<RecentMediaCoordinator> logger)
    {
        _recentContext = recentContext;
        _playQueueContext = playQueueContext;
        _libraryContext = libraryContext;
        _playlistsContext = playlistsContext;
        _generalSettings = generalSettings;
        _databaseService = databaseService;
        _mediaFactory = mediaFactory;
        _sourceMetadataService = sourceMetadataService;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _playQueueContext.PropertyChanged += PlayQueueContext_PropertyChanged;
        _playlistsContext.Playlists.CollectionChanged += Playlists_CollectionChanged;
        _generalSettings.PropertyChanged += GeneralSettings_PropertyChanged;

        if (_playQueueContext.CurrentItem is not null)
        {
            AttachCurrentItemListener(_playQueueContext.CurrentItem);
        }

        Messenger.Register<QueueSessionStartedMessage>(this);
        Messenger.Register<PropertyChangedMessage<MusicLibrary>>(this);
    }

    /// <inheritdoc/>
    public async void Receive(QueueSessionStartedMessage message)
    {
        await HandleQueueSessionStartedAsync(message.Playlist, message.IsUpgrade);
    }

    /// <inheritdoc/>
    public void Receive(PropertyChangedMessage<MusicLibrary> message)
    {
        RunOnUiThread(RelinkAllItems);
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        if (!_generalSettings.ShowRecent)
        {
            RunOnUiThread(() =>
            {
                ClearRecentCollection();
                _recentContext.IsLoaded = true;
            });
            return;
        }

        await _loadLock.WaitAsync();
        try
        {
            var entries = await _databaseService.LoadRecentEntriesAsync(RecentContext.MaxRecentItems);
            if (entries.Count == 0 && StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count > 0)
            {
                entries = await MigrateLegacyMruAsync();
            }

            RunOnUiThread(() =>
            {
                var viewModels = new List<RecentItemViewModel>(entries.Count);
                foreach (var entry in entries)
                {
                    var vm = HydrateRecentItem(entry);
                    if (vm is not null)
                    {
                        viewModels.Add(vm);
                    }
                }

                ClearRecentCollection();
                foreach (var vm in viewModels)
                {
                    _recentContext.Recent.Add(vm);
                }

                RelinkAllItems();
                UpdateActiveStates();
                _recentContext.IsLoaded = true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent media entries from SQLite.");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task LoadDetailsAndThumbnailsAsync()
    {
        if (!_generalSettings.ShowRecent) return;
        if (!_recentContext.IsLoaded)
        {
            await LoadAsync();
        }

        List<RecentItemViewModel> snapshot = _recentContext.Recent.ToList();
        var tasks = new List<Task>();
        foreach (var item in snapshot)
        {
            if (item.Kind == RecentItemKind.SingleMedia && item.Media is { } media)
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
            else if (item.Kind == RecentItemKind.Album && item.LinkedAlbum is { AlbumArt: null } album)
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
    public async Task RemoveAsync(RecentItemViewModel item)
    {
        RunOnUiThread(() =>
        {
            item.Detach();
            _recentContext.Recent.Remove(item);
        });

        try
        {
            await _databaseService.DeleteRecentEntryAsync(item.EntryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recent entry '{EntryId}'.", item.EntryId);
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        RunOnUiThread(() =>
        {
            ClearRecentCollection();
            _recentContext.IsLoaded = true;
        });

        try
        {
            await _databaseService.ClearRecentEntriesAsync();
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear recent media history.");
        }
    }

    public async Task HandleQueueSessionStartedAsync(Playlist playlist, bool isUpgrade)
    {
        if (!_generalSettings.ShowRecent || playlist.IsEmpty)
            return;

        if (!_recentContext.IsLoaded)
        {
            await LoadAsync();
        }

        IReadOnlyList<MediaViewModel> canonicalItems = playlist.GetCanonicalItems();
        if (canonicalItems.Count == 0)
            return;

        var origin = playlist.Origin ?? (canonicalItems.Count > 1
            ? PlaylistOrigin.ForAdHocPlaylist(canonicalItems[0].Name)
            : PlaylistOrigin.ForSingleMedia(canonicalItems[0]));

        List<string>? canonicalLocations = origin.Kind == RecentItemKind.AdHocPlaylist
            ? canonicalItems.Select(m => m.Location).ToList()
            : null;
        string sourceOrTitleKey = !string.IsNullOrEmpty(origin.SourceId) ? origin.SourceId : origin.Title;
        string contentHash = PlaylistOrigin.ComputeContentHash(origin.Kind, sourceOrTitleKey, canonicalLocations);

        int initialIndex = playlist.CurrentItem is not null
            ? Math.Max(0, IndexOfItem(canonicalItems, playlist.CurrentItem))
            : Math.Max(0, playlist.CurrentIndex);

        var now = DateTimeOffset.UtcNow;
        string? replaceEntryId = isUpgrade ? _lastRecordedEntryId : null;

        RecentItemViewModel? targetVm = null;
        string entryId = Guid.NewGuid().ToString();

        RunOnUiThread(() =>
        {
            if (replaceEntryId is not null)
            {
                var upgradedTarget = _recentContext.Recent.FirstOrDefault(r => r.EntryId == replaceEntryId);
                if (upgradedTarget is not null && upgradedTarget.ContentHash != contentHash)
                {
                    upgradedTarget.Detach();
                    _recentContext.Recent.Remove(upgradedTarget);
                }
            }

            var existing = _recentContext.Recent.FirstOrDefault(r => r.ContentHash == contentHash);
            if (existing is not null)
            {
                entryId = existing.EntryId;
                existing.ActiveSessionId = playlist.SessionId;
                if (origin.Kind == RecentItemKind.AdHocPlaylist)
                {
                    existing.UpdateAdHocSnapshot(origin.Title, origin.SourceId, canonicalItems, initialIndex);
                }

                int existingIndex = _recentContext.Recent.IndexOf(existing);
                if (existingIndex > 0)
                {
                    _recentContext.Recent.Move(existingIndex, 0);
                }

                targetVm = existing;
            }
            else
            {
                if (origin.Kind == RecentItemKind.SingleMedia)
                {
                    var media = playlist.CurrentItem ?? canonicalItems[0];
                    targetVm = new RecentItemViewModel(entryId, contentHash, media, playlist.SessionId);
                }
                else
                {
                    IReadOnlyList<MediaViewModel> snapshot = origin.Kind == RecentItemKind.AdHocPlaylist
                        ? canonicalItems
                        : [];
                    targetVm = new RecentItemViewModel(
                        entryId,
                        contentHash,
                        origin.Kind,
                        origin.Title,
                        origin.Subtitle,
                        origin.SourceId,
                        snapshot,
                        initialIndex,
                        playlist.SessionId);
                }

                LinkItem(targetVm);
                _recentContext.Recent.Insert(0, targetVm);

                while (_recentContext.Recent.Count > RecentContext.MaxRecentItems)
                {
                    var tail = _recentContext.Recent[^1];
                    tail.Detach();
                    _recentContext.Recent.RemoveAt(_recentContext.Recent.Count - 1);
                }
            }

            _lastRecordedEntryId = entryId;
            UpdateActiveStates();
        });

        try
        {
            List<RawMediaRecordDto> dtoItems = origin.Kind switch
            {
                RecentItemKind.SingleMedia => [ToRawMediaRecord(playlist.CurrentItem ?? canonicalItems[0])],
                RecentItemKind.AdHocPlaylist => canonicalItems.Select(ToRawMediaRecord).ToList(),
                _ => []
            };

            var dto = new RecentEntryDto
            {
                Id = entryId,
                Kind = origin.Kind,
                ContentHash = contentHash,
                Title = origin.Title,
                Subtitle = origin.Subtitle,
                SourceId = origin.SourceId,
                LastPlayed = now,
                Items = dtoItems
            };

            await _databaseService.UpsertRecentEntryAsync(dto, replaceEntryId, RecentContext.MaxRecentItems);

            if (targetVm is { Kind: RecentItemKind.SingleMedia, Media: { } singleMedia })
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

    private void PlayQueueContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayQueueContext.CurrentItem):
                AttachCurrentItemListener(_playQueueContext.CurrentItem);
                RunOnUiThread(UpdateActiveStates);
                break;

            case nameof(PlayQueueContext.ActiveSessionId):
            case nameof(PlayQueueContext.ActiveOrigin):
                RunOnUiThread(UpdateActiveStates);
                break;
        }
    }

    private void AttachCurrentItemListener(MediaViewModel? newItem)
    {
        if (ReferenceEquals(_monitoredCurrentItem, newItem)) return;

        if (_monitoredCurrentItem is not null)
        {
            _monitoredCurrentItem.PropertyChanged -= MonitoredCurrentItem_PropertyChanged;
        }

        _monitoredCurrentItem = newItem;

        if (_monitoredCurrentItem is not null)
        {
            _monitoredCurrentItem.PropertyChanged += MonitoredCurrentItem_PropertyChanged;
        }
    }

    private void MonitoredCurrentItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MediaViewModel.IsPlaying) or nameof(MediaViewModel.IsMediaActive))
        {
            RunOnUiThread(UpdateActiveStates);
        }
    }

    private void UpdateActiveStates()
    {
        Guid activeSessionId = _playQueueContext.ActiveSessionId;
        PlaylistOrigin? activeOrigin = _playQueueContext.ActiveOrigin;
        MediaViewModel? currentItem = _playQueueContext.CurrentItem;
        bool isPlaying = currentItem?.IsPlaying ?? false;

        foreach (var item in _recentContext.Recent)
        {
            bool isItemActive;
            if (activeSessionId == Guid.Empty || currentItem is null)
            {
                isItemActive = false;
            }
            else if (item.Kind == RecentItemKind.SingleMedia)
            {
                bool isIndividualMode = activeOrigin is null || activeOrigin.TrackItemsIndividually;
                isItemActive = isIndividualMode &&
                               item.Media is not null &&
                               string.Equals(
                                   PlaylistOrigin.NormalizeLocation(item.Media.Location),
                                   PlaylistOrigin.NormalizeLocation(currentItem.Location),
                                   StringComparison.Ordinal);
            }
            else
            {
                isItemActive = item.ActiveSessionId == activeSessionId &&
                               activeOrigin is { TrackItemsIndividually: false };
            }

            item.IsQueueActive = isItemActive;
            item.IsPlaying = isItemActive && isPlaying;
        }
    }

    private void Playlists_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(RelinkAllItems);
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
                RunOnUiThread(() =>
                {
                    ClearRecentCollection();
                    _recentContext.IsLoaded = true;
                });
            }
        }
    }

    private void RelinkAllItems()
    {
        foreach (var item in _recentContext.Recent)
        {
            LinkItem(item);
        }
    }

    private void LinkItem(RecentItemViewModel item)
    {
        switch (item.Kind)
        {
            case RecentItemKind.UserPlaylist:
                {
                    var playlist = _playlistsContext.Playlists.FirstOrDefault(p => p.Id == item.SourceId);
                    item.LinkPlaylist(playlist);
                }
                break;

            case RecentItemKind.Album:
                {
                    AlbumViewModel? album = null;
                    if (item.SourceId is not null)
                    {
                        _libraryContext.Music.Albums.TryGetValue(item.SourceId, out album);
                    }

                    item.LinkAlbum(album);
                }
                break;

            case RecentItemKind.Artist:
                {
                    ArtistViewModel? artist = null;
                    if (item.SourceId is not null)
                    {
                        _libraryContext.Music.Artists.TryGetValue(item.SourceId, out artist);
                    }

                    item.LinkArtist(artist);
                }
                break;
        }
    }

    private RecentItemViewModel? HydrateRecentItem(RecentEntryDto entry)
    {
        if (entry.Kind == RecentItemKind.SingleMedia)
        {
            var first = entry.Items.FirstOrDefault();
            string? path = first?.Path ?? entry.SourceId;
            if (string.IsNullOrEmpty(path)) return null;

            var media = HydrateMediaViewModel(first ?? new RawMediaRecordDto { Path = path, Title = entry.Title });
            if (media is null) return null;

            return new RecentItemViewModel(entry.Id, entry.ContentHash, media);
        }

        if (entry.Kind == RecentItemKind.AdHocPlaylist)
        {
            var snapshot = new List<MediaViewModel>(entry.Items.Count);
            foreach (var itemDto in entry.Items)
            {
                var media = HydrateMediaViewModel(itemDto);
                if (media is not null)
                {
                    snapshot.Add(media);
                }
            }

            return new RecentItemViewModel(
                entry.Id,
                entry.ContentHash,
                entry.Kind,
                entry.Title,
                entry.Subtitle,
                entry.SourceId,
                snapshot);
        }

        return new RecentItemViewModel(
            entry.Id,
            entry.ContentHash,
            entry.Kind,
            entry.Title,
            entry.Subtitle,
            entry.SourceId,
            []);
    }

    private MediaViewModel? HydrateMediaViewModel(RawMediaRecordDto record)
    {
        if (string.IsNullOrEmpty(record.Path))
            return null;

        MediaViewModel media;
        bool existing = false;

        if (Uri.TryCreate(record.Path, UriKind.Absolute, out Uri? uri))
        {
            existing = _mediaFactory.TryGetOrCreate(uri, out media);
        }
        else
        {
            media = _mediaFactory.GetOrCreate(record.Path);
        }

        if (!existing)
        {
            if (!string.IsNullOrEmpty(record.Title))
            {
                media.Name = record.Title;
            }

            media.MediaInfo = CreateMediaInfo(record);
            if (record.DateAdded != default)
            {
                media.DateAdded = record.DateAdded.ToLocalTime();
            }
        }

        return media;
    }

    private static MediaInfo CreateMediaInfo(RawMediaRecordDto record)
    {
        if (record.MediaType == MediaPlaybackType.Music)
        {
            return new MediaInfo(new MusicInfo
            {
                Title = record.Title,
                Artist = record.Artist,
                Album = record.Album,
                AlbumArtist = record.AlbumArtist,
                Composers = record.Composers,
                Genre = record.Genre,
                TrackNumber = record.TrackNumber,
                Year = record.Year,
                Duration = record.Duration,
                Bitrate = record.Bitrate,
            });
        }

        if (record.MediaType == MediaPlaybackType.Video)
        {
            return new MediaInfo(new VideoInfo
            {
                Title = record.Title,
                Subtitle = record.Subtitle,
                Producers = record.Producers,
                Writers = record.Writers,
                Year = record.Year,
                Duration = record.Duration,
                Width = record.Width,
                Height = record.Height,
                Bitrate = record.VideoBitrate,
            });
        }

        return new MediaInfo(record.MediaType, record.Title, record.Year, record.Duration);
    }

    private static RawMediaRecordDto ToRawMediaRecord(MediaViewModel media)
    {
        return new RawMediaRecordDto
        {
            Path = media.Location,
            Title = media.Name,
            MediaType = media.MediaType,
            Duration = media.Duration
        };
    }

    private static int IndexOfItem(IReadOnlyList<MediaViewModel> items, MediaViewModel target)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], target) ||
                string.Equals(items[i].Location, target.Location, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    private async Task<List<RecentEntryDto>> MigrateLegacyMruAsync()
    {
        var migrated = new List<RecentEntryDto>();
        try
        {
            var mruEntries = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderBy(x => x.Metadata)
                .ToArray();

            foreach (var entry in mruEntries)
            {
                if (string.IsNullOrEmpty(entry.Token)) continue;
                try
                {
                    StorageFile file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(
                        entry.Token,
                        AccessCacheOptions.SuppressAccessTimeUpdate);

                    if (file.IsSupportedPlaylist()) continue;

                    MediaViewModel media = _mediaFactory.GetOrCreate(file);
                    string contentHash = PlaylistOrigin.ComputeContentHash(RecentItemKind.SingleMedia, media.Location);
                    DateTimeOffset lastPlayed = long.TryParse(entry.Metadata, out long ticks)
                        ? new DateTimeOffset(ticks, TimeSpan.Zero)
                        : DateTimeOffset.UtcNow;

                    var dto = new RecentEntryDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Kind = RecentItemKind.SingleMedia,
                        ContentHash = contentHash,
                        Title = media.Name,
                        SourceId = media.Location,
                        LastPlayed = lastPlayed,
                        Items = [ToRawMediaRecord(media)]
                    };

                    await _databaseService.UpsertRecentEntryAsync(dto, maxEntries: RecentContext.MaxRecentItems);
                }
                catch
                {
                    // Skip inaccessible legacy MRU token
                }
            }

            migrated = await _databaseService.LoadRecentEntriesAsync(RecentContext.MaxRecentItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate legacy MRU entries.");
        }

        return migrated;
    }

    private void ClearRecentCollection()
    {
        foreach (var item in _recentContext.Recent)
        {
            item.Detach();
        }

        _recentContext.Recent.Clear();
    }

    private void RunOnUiThread(Action action)
    {
        if (_dispatcherQueue is null || _dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
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
