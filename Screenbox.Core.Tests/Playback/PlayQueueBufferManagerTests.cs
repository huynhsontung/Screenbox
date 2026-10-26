using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Core;
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using IMediaSource = Screenbox.Core.MediaSources.IMediaSource;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.Playback;

public partial class PlayQueueBufferManagerTests
{
    private sealed partial class DummyPlaybackItem : IPlaybackItem
    {
        public bool IsDisposed { get; private set; }
        public IMediaSource Source { get; } = null!;
        public TimeSpan? Duration => TimeSpan.Zero;
        public ISingleSelectMediaTrackList<AudioTrack> AudioTracks => throw new NotImplementedException();
        public ISingleSelectMediaTrackList<VideoTrack> VideoTracks => throw new NotImplementedException();
        public ISubtitleTrackList SubtitleTracks => throw new NotImplementedException();
        public IReadOnlyList<ChapterCue> Chapters => Array.Empty<ChapterCue>();

        public Task WaitForParsedAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ExpandedMediaItem>> ExpandSubItemsAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpandedMediaItem>>(Array.Empty<ExpandedMediaItem>());

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class MockPlayerService : IPlayerService
    {
        public int CreateCount { get; private set; }
        public List<DummyPlaybackItem> CreatedItems { get; } = new();

        public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

        public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options)
        {
            CreateCount++;
            var item = new DummyPlaybackItem();
            CreatedItems.Add(item);
            return item;
        }

        public void DisposePlayer(IMediaPlayer player) { }
    }

    private static MediaViewModel CreateMedia(string name) =>
        new(new UriMediaSource(new Uri($"https://example.com/{name}.mp3"))) { Name = name };

    [Test]
    public async Task GetOrCreatePlaybackItem_CachesAndReturnsSameItem()
    {
        var playerService = new MockPlayerService();
        var playlistService = new PlaylistService(null!, null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, playlistService);
        var player = new TestMediaPlayer();
        var media = CreateMedia("test1");

        var item1 = bufferManager.GetOrCreatePlaybackItem(player, media);
        var item2 = bufferManager.GetOrCreatePlaybackItem(player, media);

        await Assert.That(item1).IsNotNull();
        await Assert.That(item1).IsSameReferenceAs(item2);
        await Assert.That(playerService.CreateCount).IsEqualTo(1);
    }

    [Test]
    public async Task CleanPlaybackItem_DisposesAndRemovesItem()
    {
        var playerService = new MockPlayerService();
        var playlistService = new PlaylistService(null!, null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, playlistService);
        var player = new TestMediaPlayer();
        var media = CreateMedia("test1");

        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, media)!;
        await Assert.That(item1.IsDisposed).IsFalse();

        bufferManager.CleanPlaybackItem(media);
        await Assert.That(item1.IsDisposed).IsTrue();

        var item2 = bufferManager.GetOrCreatePlaybackItem(player, media);
        await Assert.That(item2).IsNotSameReferenceAs(item1);
        await Assert.That(playerService.CreateCount).IsEqualTo(2);
    }

    [Test]
    public async Task Clear_DisposesAllCachedItems()
    {
        var playerService = new MockPlayerService();
        var playlistService = new PlaylistService(null!, null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, playlistService);
        var player = new TestMediaPlayer();
        var media1 = CreateMedia("test1");
        var media2 = CreateMedia("test2");

        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, media1)!;
        var item2 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, media2)!;

        bufferManager.Clear();

        await Assert.That(item1.IsDisposed).IsTrue();
        await Assert.That(item2.IsDisposed).IsTrue();
    }

    [Test]
    public async Task UpdateBufferAsync_EvictsOutOfWindowItems()
    {
        var playerService = new MockPlayerService();
        var playlistService = new PlaylistService(null!, null!, null!);
        using var bufferManager = new PlayQueueBufferManager(playerService, playlistService);
        var player = new TestMediaPlayer();

        var items = new List<MediaViewModel>
        {
            CreateMedia("song0"),
            CreateMedia("song1"),
            CreateMedia("song2"),
            CreateMedia("song3"),
            CreateMedia("song4")
        };

        var playlist = new Playlist(items[0], items);

        // At index 0, buffer window is [0, 1, 2]
        bufferManager.UpdateBuffer(player, playlist, MediaPlaybackAutoRepeatMode.None);
        var item0 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, items[0])!;
        var item1 = (DummyPlaybackItem)bufferManager.GetOrCreatePlaybackItem(player, items[1])!;
        await Assert.That(item0.IsDisposed).IsFalse();
        await Assert.That(item1.IsDisposed).IsFalse();

        // Slide window to index 4: buffer window is [2, 3, 4]
        playlist.CurrentIndex = 4;
        bufferManager.UpdateBuffer(player, playlist, MediaPlaybackAutoRepeatMode.None);

        // Items 0 and 1 slid out of window -> must be disposed
        await Assert.That(item0.IsDisposed).IsTrue();
        await Assert.That(item1.IsDisposed).IsTrue();
    }
}
