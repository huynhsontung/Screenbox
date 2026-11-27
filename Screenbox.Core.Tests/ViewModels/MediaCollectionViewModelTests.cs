using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Tests.ViewModels;

public sealed class MediaCollectionViewModelTests
{
    private sealed class StubQueueService : IQueueService
    {
        private readonly QueueSession? _session;

        public StubQueueService(QueueSession? session = null)
        {
            _session = session;
        }

        public Task<QueueSession?> TryCreateQueueAsync(
            IReadOnlyList<IStorageItem> storageItems,
            StorageFileQueryResult? neighboringFilesQuery = null,
            CancellationToken cancellationToken = default) => Task.FromResult(_session);

        public Task<QueueSession?> TryParseQueueAsync(
            IReadOnlyList<IStorageItem> storageItems,
            StorageFile? playNext = null,
            bool expandPlaylists = true,
            CancellationToken cancellationToken = default) => Task.FromResult(_session);

        public Task<QueueSession> ParseQueueAsync(MediaViewModel media, CancellationToken cancellationToken = default) =>
            Task.FromResult(_session ?? new QueueSession(media));

        public Task<QueueSession> ParseQueueAsync(StorageFile file, CancellationToken cancellationToken = default) =>
            Task.FromResult(_session ?? new QueueSession());

        public string? LastResolvedPath { get; private set; }
        public string? LastResolvedToken { get; private set; }

        public Task<QueueSession> ParseQueueAsync(Uri uri, CancellationToken cancellationToken = default) =>
            Task.FromResult(_session ?? new QueueSession());

        public Task<QueueSession?> TryResolveCollectionQueueAsync(
            string sourcePath,
            string? token = null,
            CancellationToken cancellationToken = default)
        {
            LastResolvedPath = sourcePath;
            LastResolvedToken = token;
            return Task.FromResult(_session);
        }

        public Task<QueueSession> AddNeighboringFilesAsync(QueueSession session, CancellationToken cancellationToken = default) =>
            Task.FromResult(session);

        public QueueSession Shuffle(QueueSession session, int? preserveIndex = null) => session;

        public QueueSession RestoreFromShuffle(QueueSession session) => session;

        public void UpdateShuffleBackup(QueueSession session, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }

        public IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int queueCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5) =>
            Array.Empty<int>();
    }

    private static MediaViewModel CreateDummyMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task Constructor_DetectsFolderVsPlaylistFilePath()
    {
        var messenger = new StrongReferenceMessenger();
        var folderVm = new MediaCollectionViewModel("Rock", @"C:\Music\Rock", null, token: "tok1", messenger);
        var m3uVm = new MediaCollectionViewModel("Favorites", @"C:\Playlists\fav.m3u8", null, token: "tok2", messenger);
        var uriVm = new MediaCollectionViewModel("Stream", "https://example.com/live.m3u8", null, messenger: messenger);

        await Assert.That(folderVm.IsPlaylistFile).IsFalse();
        await Assert.That(m3uVm.IsPlaylistFile).IsTrue();
        await Assert.That(uriVm.IsPlaylistFile).IsTrue();
        await Assert.That(folderVm.Token).IsEqualTo("tok1");
    }

    [Test]
    public async Task QueuePlaybackStateChanged_SyncsActiveAndPlayingWhenOriginMatches()
    {
        var messenger = new StrongReferenceMessenger();
        var collection = new MediaCollectionViewModel("Rock", @"C:\Music\Rock", null, messenger: messenger);
        var song = CreateDummyMedia("song1");
        var sessionId = Guid.NewGuid();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, QueueOrigin.ForMediaCollection("Rock", @"c:\music\rock"), song, IsPlaying: true)));

        await Assert.That(collection.IsQueueActive).IsTrue();
        await Assert.That(collection.IsPlaying).IsTrue();

        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(sessionId, QueueOrigin.ForSingleMedia(song), song, IsPlaying: true)));

        await Assert.That(collection.IsQueueActive).IsFalse();
        await Assert.That(collection.IsPlaying).IsFalse();
    }

    [Test]
    public async Task PlayAsync_WhenActive_TogglesPlayPause_WhenInactive_ResolvesLiveUriPlaylist()
    {
        var messenger = new StrongReferenceMessenger();
        var track1 = CreateDummyMedia("track1");
        var track2 = CreateDummyMedia("track2");
        var liveSession = new QueueSession(0, new[] { track1, track2 });
        var queueService = new StubQueueService(liveSession);

        var collection = new MediaCollectionViewModel(
            "Radio",
            "https://example.com/playlist.m3u8",
            queueService,
            messenger: messenger);

        SetQueueMessage? sentQueue = null;
        bool toggledPlayPause = false;
        messenger.Register<SetQueueMessage>(this, (_, msg) => sentQueue = msg);
        messenger.Register<TogglePlayPauseMessage>(this, (_, _) => toggledPlayPause = true);

        await collection.PlayAsync();

        await Assert.That(sentQueue).IsNotNull();
        await Assert.That(sentQueue!.ShouldPlay).IsTrue();
        await Assert.That(sentQueue.Value.CurrentItem?.Origin?.Kind).IsEqualTo(RecentItemKind.MediaCollection);
        await Assert.That(sentQueue.Value.CurrentItem?.Origin?.SourceId).IsEqualTo("https://example.com/playlist.m3u8");
        await Assert.That(sentQueue.Value.Items.Count).IsEqualTo(2);

        // Simulate active state -> PlayAsync toggles play/pause instead of recreating queue
        messenger.Send(new QueuePlaybackStateChangedMessage(
            new QueuePlaybackState(liveSession.SessionId, sentQueue.Value.CurrentItem?.Origin, track1, IsPlaying: true)));

        await collection.PlayAsync();
        await Assert.That(toggledPlayPause).IsTrue();
    }

    [Test]
    public async Task GetPlayableItemsAsync_DelegatesResolutionToQueueService()
    {
        var messenger = new StrongReferenceMessenger();
        var track1 = CreateDummyMedia("track1");
        var liveSession = new QueueSession(0, new[] { track1 });
        var queueService = new StubQueueService(liveSession);

        var collection = new MediaCollectionViewModel(
            "Rock",
            @"C:\Music\Rock",
            queueService,
            token: "tok-rock",
            messenger: messenger);

        IReadOnlyList<MediaViewModel> items = await collection.GetPlayableItemsAsync();

        await Assert.That(items.Count).IsEqualTo(1);
        await Assert.That(items[0]).IsSameReferenceAs(track1);
        await Assert.That(queueService.LastResolvedPath).IsEqualTo(@"C:\Music\Rock");
        await Assert.That(queueService.LastResolvedToken).IsEqualTo("tok-rock");
    }
}
