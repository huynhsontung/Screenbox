using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
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
using MediaPlaybackType = Screenbox.Core.Enums.MediaPlaybackType;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

namespace Screenbox.Core.Tests.Services;

public partial class VlcMediaProbeServiceTests
{
    private sealed class FakePlayerService : IPlayerService
    {
        public int CreateCount { get; private set; }
        public string[]? LastOptions { get; private set; }

        public IMediaPlayer Initialize(string[] swapChainOptions) => throw new NotImplementedException();

        public IPlaybackItem CreatePlaybackItem(IMediaPlayer player, IMediaSource source, params string[] options)
        {
            CreateCount++;
            LastOptions = options;
            return new DummyPlaybackItem();
        }

        public void DisposePlayer(IMediaPlayer player) { }
    }

    private sealed partial class DummyPlaybackItem : IPlaybackItem
    {
        public bool IsDisposed { get; private set; }
        public IMediaSource Source { get; init; } = null!;
        public TimeSpan? Duration => null;
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

    private sealed partial class FakeBufferManager : IPlayQueueBufferManager
    {
        public System.Collections.Generic.IReadOnlyList<MediaViewModel> BufferedItems => Array.Empty<MediaViewModel>();

        public bool TryGetPlaybackItem(IMediaSource source, [NotNullWhen(true)] out IPlaybackItem? item)
        {
            item = null;
            return false;
        }

        public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm) => null;
        public void CleanPlaybackItem(MediaViewModel vm) { }
        public void Clear() { }
        public void UpdateBuffer(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode) { }
        public void Dispose() { }
    }

    [Test]
    public async Task ProbeMediaAsync_WithNullArguments_ShouldThrowArgumentNullException()
    {
        var service = new VlcMediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<VlcMediaProbeService>.Instance);

        await Assert.That(async () => await service.ProbeMediaAsync((IMediaSource)null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await service.ProbeMediaAsync((IPlaybackItem)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WithUnsupportedPlaybackItem_ThrowsNotSupportedException()
    {
        var service = new VlcMediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<VlcMediaProbeService>.Instance);

        var dummyItem = new DummyPlaybackItem();

        await Assert.That(async () => await service.ProbeMediaAsync(dummyItem)).Throws<NotSupportedException>();
    }

    [Test]
    public async Task ProbeMediaAsync_WhenNoMediaPlayer_ReturnsNull()
    {
        var source = new UriMediaSource(new Uri("https://example.com/video.mp4"));
        var service = new VlcMediaProbeService(
            new PlayerContext(),
            new FakePlayerService(),
            new FakeBufferManager(),
            NullLogger<VlcMediaProbeService>.Instance);

        var result = await service.ProbeMediaAsync(source);

        await Assert.That(result).IsNull();
    }
}
