using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Factories;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Screenbox.Core.Tests.Coordinators;

public sealed partial class PlayQueueCoordinatorTests
{
    private sealed class StubFilesService : IFilesService
    {
        public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null) => Task.FromResult<StorageFileQueryResult?>(null);
        public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => Task.FromResult<StorageFile?>(null);
        public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery) => Task.FromResult<StorageFile?>(null);
        public StorageItemQueryResult GetSupportedItems(StorageFolder folder) => throw new NotSupportedException();
        public Windows.Foundation.IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder) => throw new NotSupportedException();
        public Windows.Foundation.IAsyncOperation<StorageFile> PickFileAsync(params string[] formats) => throw new NotSupportedException();
        public Windows.Foundation.IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats) => throw new NotSupportedException();
        public Windows.Foundation.IAsyncOperation<StorageFile> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypes, PickerLocationId startLocation = PickerLocationId.ComputerFolder) => throw new NotSupportedException();
        public Windows.Foundation.IAsyncOperation<StorageFolder> PickFolderAsync() => throw new NotSupportedException();
        public Task OpenFileLocationAsync(string path) => Task.CompletedTask;
        public Task OpenFileLocationAsync(StorageFile file) => Task.CompletedTask;
        public string? AddToRecent(IStorageItem item) => null;
        public Windows.Foundation.IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token) => throw new NotSupportedException();
    }

    private sealed partial class StubSmtcService : ISystemMediaTransportControlsService, IDisposable
    {
        private readonly MediaPlayer _winRtPlayer = new();
        public SystemMediaTransportControls TransportControls => _winRtPlayer.SystemMediaTransportControls;
        public Task UpdateTransportControlsDisplayAsync(MediaViewModel? item) => Task.CompletedTask;
        public void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime) { }
        public void UpdatePlaybackStatus(MediaPlaybackState state) { }
        public void ClosePlayback() { }
        public void Dispose() => _winRtPlayer.Dispose();
    }

    private sealed partial class StubBufferManager : IPlayQueueBufferManager
    {
        public IReadOnlyList<QueueItemViewModel> BufferedItems => Array.Empty<QueueItemViewModel>();
        public bool TryGetPlaybackItem(QueueItemViewModel item, [NotNullWhen(true)] out IPlaybackItem? playbackItem)
        {
            playbackItem = null;
            return false;
        }
        public bool TryGetPlaybackItem(MediaViewModel vm, [NotNullWhen(true)] out IPlaybackItem? item)
        {
            item = null;
            return false;
        }
        public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, QueueItemViewModel item) => null;
        public void CleanPlaybackItem(QueueItemViewModel item) { }
        public void CleanPlaybackItems(MediaViewModel media, QueueItemViewModel? exclude = null) { }
        public void Clear() { }
        public void UpdateBuffer(IMediaPlayer? player, QueueSession session, MediaPlaybackAutoRepeatMode repeatMode) { }
        public void Dispose() { }
    }

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    private static (
        PlayQueueCoordinator Coordinator,
        PlayQueueContext Context,
        TestMediaPlayer Player,
        StubSmtcService Smtc,
        StrongReferenceMessenger Messenger) CreatePlayQueueCoordinator()
    {
        var messenger = new StrongReferenceMessenger();
        var context = new PlayQueueContext();
        var libraryContext = new LibraryContext();
        var mediaFactory = new MediaViewModelFactory(new MediaSourceFactory(), libraryContext);
        var filesService = new StubFilesService();
        var settingsStorage = new MemorySettingsStorage();
        var generalSettings = new GeneralSettingsContext(settingsStorage) { ShowRecent = true };
        var playbackSettings = new PlaybackSettingsContext(settingsStorage);
        var queueService = new QueueService(mediaFactory, filesService, generalSettings);
        var playbackControl = new PlaybackControlService(filesService, queueService);
        var smtc = new StubSmtcService();
        var testPlayer = new TestMediaPlayer();
        var playerContext = new PlayerContext { MediaPlayer = testPlayer };
        var bufferManager = new StubBufferManager();

        var coordinator = new PlayQueueCoordinator(
            context,
            queueService,
            playbackControl,
            playbackSettings,
            smtc,
            mediaFactory,
            playerContext,
            bufferManager,
            NullLogger<PlayQueueCoordinator>.Instance,
            messenger);

        return (coordinator, context, testPlayer, smtc, messenger);
    }

    [Test]
    public async Task QueueMutations_PreservePerItemOrigins_AndTrackMixedOriginTransitions()
    {
        var (coordinator, context, player, smtc, messenger) = CreatePlayQueueCoordinator();
        using var _ = smtc;

        var received = new List<QueueSessionStartedMessage>();
        messenger.Register<QueueSessionStartedMessage>(this, (_, msg) => received.Add(msg));

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var extra = CreateDummyMedia("extra");
        var playlistTrack = CreateDummyMedia("playlistTrack");
        var album = new AlbumViewModel("AlbumA", "ArtistA", messenger);
        var albumOrigin = QueueOrigin.ForAlbum(album);
        var playlist = new PlaylistViewModel(null!, null!, NullLogger<PlaylistViewModel>.Instance, messenger) { Name = "Roadtrip" };
        var playlistOrigin = QueueOrigin.ForPlaylist(playlist);

        var session = new QueueSession(0, new[] { track1, track2 }, origin: albumOrigin);
        messenger.Send(new SetQueueMessage(session, shouldPlay: true));

        await Assert.That(context.ActiveOrigin).IsEqualTo(albumOrigin);
        await Assert.That(received.Count).IsEqualTo(1);
        await Assert.That(received[0].Origin).IsEqualTo(albumOrigin);

        // Shuffle preserves per-item origin
        context.ShuffleMode = true;
        await Assert.That(context.ActiveOrigin).IsEqualTo(albumOrigin);

        context.ShuffleMode = false;
        await Assert.That(context.ActiveOrigin).IsEqualTo(albumOrigin);

        // InsertNext single item and Enqueue playlist item without unlinking active album item
        coordinator.InsertNext(extra);
        coordinator.Enqueue(new[] { new QueueItemViewModel(playlistTrack, playlistOrigin) });
        await Assert.That(context.ActiveOrigin).IsEqualTo(albumOrigin);
        await Assert.That(received.Count).IsEqualTo(1);

        // Advance to inserted single item -> ActiveOrigin becomes null, records SingleMedia
        await coordinator.NextAsync();
        await Assert.That(context.CurrentItem?.Media).IsSameReferenceAs(extra);
        await Assert.That(context.ActiveOrigin).IsNull();
        await Assert.That(received.Count).IsEqualTo(2);
        await Assert.That(received[1].Origin).IsEqualTo(QueueOrigin.ForSingleMedia(extra));
        await Assert.That(received[1].Media).IsSameReferenceAs(extra);

        // Advance to track2 (still from AlbumA) -> ActiveOrigin returns to AlbumA, promotes AlbumA
        await coordinator.NextAsync();
        await Assert.That(context.CurrentItem?.Media).IsSameReferenceAs(track2);
        await Assert.That(context.ActiveOrigin).IsEqualTo(albumOrigin);
        await Assert.That(received.Count).IsEqualTo(3);
        await Assert.That(received[2].Origin).IsEqualTo(albumOrigin);

        // Advance to enqueued playlistTrack -> ActiveOrigin switches to Playlist, records Playlist
        await coordinator.NextAsync();
        await Assert.That(context.CurrentItem?.Media).IsSameReferenceAs(playlistTrack);
        await Assert.That(context.ActiveOrigin).IsEqualTo(playlistOrigin);
        await Assert.That(received.Count).IsEqualTo(4);
        await Assert.That(received[3].Origin).IsEqualTo(playlistOrigin);

        // Removing another item keeps active item's origin intact
        coordinator.Remove(context.Items[0]);
        await Assert.That(context.ActiveOrigin).IsEqualTo(playlistOrigin);
    }

    [Test]
    public async Task RecentMediaItemViewModel_IsPlaying_FalseForContainerItem_TrueForIndividualItem()
    {
        var (coordinator, context, player, smtc, messenger) = CreatePlayQueueCoordinator();
        using var _ = smtc;

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var recentTrack1 = new RecentMediaItemViewModel(track1, messenger);
        var recentTrack2 = new RecentMediaItemViewModel(track2, messenger);
        var album = new AlbumViewModel("AlbumA", "ArtistA", messenger);

        player.PlaybackState = MediaPlaybackState.Playing;

        // Container item -> Media.IsPlaying is true, RecentMediaItemViewModel.IsPlaying is false
        var albumSession = new QueueSession(0, new[] { track1, track2 }, origin: QueueOrigin.ForAlbum(album));
        messenger.Send(new SetQueueMessage(albumSession, shouldPlay: true));

        await Assert.That(track1.IsPlaying).IsTrue();
        await Assert.That(recentTrack1.IsMediaActive).IsFalse();
        await Assert.That(recentTrack1.IsPlaying).IsFalse();

        // Individual item -> both Media.IsPlaying and RecentMediaItemViewModel.IsPlaying are true
        var singleSession = new QueueSession(track2);
        messenger.Send(new SetQueueMessage(singleSession, shouldPlay: true));

        await Assert.That(recentTrack1.IsPlaying).IsFalse();
        await Assert.That(track2.IsPlaying).IsTrue();
        await Assert.That(recentTrack2.IsMediaActive).IsTrue();
        await Assert.That(recentTrack2.IsPlaying).IsTrue();
    }

    [Test]
    public async Task SetQueue_WhenUpgradingSessionFromSingleToFolder_EmitsReplacedEntryId()
    {
        var (_, _, _, smtc, messenger) = CreatePlayQueueCoordinator();
        using var _ = smtc;

        var received = new List<QueueSessionStartedMessage>();
        messenger.Register<QueueSessionStartedMessage>(this, (_, msg) => received.Add(msg));

        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");

        var singleSession = new QueueSession(track1);
        messenger.Send(new SetQueueMessage(singleSession, shouldPlay: false));

        var folderOrigin = QueueOrigin.ForMediaCollection("FolderA", @"C:\FolderA");
        var existingSlot = singleSession.CurrentItem!;
        var upgradedSession = new QueueSession(
            existingSlot,
            new[] { existingSlot, new QueueItemViewModel(track2) },
            singleSession);
        upgradedSession.SetOrigin(folderOrigin);
        messenger.Send(new SetQueueMessage(upgradedSession, shouldPlay: false));

        await Assert.That(received.Count).IsEqualTo(2);
        await Assert.That(received[0].ReplacedEntryId).IsNull();
        await Assert.That(received[1].Origin).IsEqualTo(folderOrigin);
        await Assert.That(received[1].ReplacedEntryId).IsEqualTo(QueueOrigin.ForSingleMedia(track1).GetEntryId());
    }
}
