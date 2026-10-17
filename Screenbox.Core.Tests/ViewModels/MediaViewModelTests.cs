using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class MediaViewModelTests
{
    [Test]
    public async Task UpdateParsedMediaInfo_ShouldMergePropertiesAndUpdateCaptions()
    {
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), new Uri("file:///c:/music/track01.flac"));
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
            {
                changedProperties.Add(e.PropertyName);
            }
        };

        var parsedMusic = new MusicInfo
        {
            Title = "Parsed Song",
            Artist = "Parsed Artist",
            Album = "Parsed Album",
            TrackNumber = 3,
            Year = 2024,
            Duration = TimeSpan.FromMinutes(4),
            Bitrate = 1000000
        };
        var parsedInfo = new MediaInfo(MediaPlaybackType.Music, parsedMusic, new VideoInfo());

        vm.UpdateParsedMediaInfo(parsedInfo);

        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Parsed Song");
        await Assert.That(vm.Caption).IsEqualTo("Parsed Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Parsed Artist – Parsed Album");
        await Assert.That(vm.Duration).IsEqualTo(TimeSpan.FromMinutes(4));
        await Assert.That(vm.TrackNumberText).IsEqualTo("3");
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaInfo));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.Duration));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaType));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.TrackNumberText));
    }

    [Test]
    public async Task PlaybackItem_WhenMediaParsedEventFires_ShouldOpportunisticallyUpdateMediaInfo()
    {
        var uri = new Uri("http://example.com/stream.mp3");
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), uri);
        var parsedMusic = new MusicInfo
        {
            Title = "Live Stream Track",
            Artist = "Stream Artist",
            Album = "Stream Album",
            Duration = TimeSpan.FromSeconds(210),
            Bitrate = 256000
        };
        var parsedInfo = new MediaInfo(MediaPlaybackType.Music, parsedMusic, new VideoInfo());
        var playbackItem = new PlaybackItem(uri, parsedInfo, isParsed: false);

        vm.SetPlaybackItem(playbackItem);
        await Assert.That(vm.IsMediaInfoParsed).IsFalse();

        playbackItem.NotifyTrackInfoLoaded();

        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Live Stream Track");
        await Assert.That(vm.Caption).IsEqualTo("Stream Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Stream Artist – Stream Album");
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(256000U);
    }

    [Test]
    public async Task ParseDetailsAsync_WhenNotYetParsed_ShouldInvokeProbeServiceAndMergeProperties()
    {
        var uri = new Uri("file:///c:/videos/movie.mkv");
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), uri);
        var probedVideo = new VideoInfo
        {
            Title = "Probed Movie",
            Subtitle = "Director's Cut",
            Year = 2025,
            Duration = TimeSpan.FromMinutes(120),
            Width = 3840,
            Height = 2160,
            Bitrate = 15000000
        };
        var probedMusic = new MusicInfo
        {
            Bitrate = 448000
        };
        var probeService = new FakeMediaProbeService(new MediaInfo(MediaPlaybackType.Video, probedMusic, probedVideo));

        await vm.ParseDetailsAsync(probeService);

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(vm.IsMediaInfoParsed).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(vm.Name).IsEqualTo("Probed Movie");
        await Assert.That(vm.MediaInfo.VideoProperties.Width).IsEqualTo(3840U);
        await Assert.That(vm.MediaInfo.VideoProperties.Height).IsEqualTo(2160U);
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(15000000U);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(448000U);

        await vm.ParseDetailsAsync(probeService);
        await Assert.That(probeService.CallCount).IsEqualTo(1);
    }

    private sealed class FakeMediaProbeService : IMediaProbeService
    {
        private readonly MediaInfo? _result;

        public int CallCount { get; private set; }

        public FakeMediaProbeService(MediaInfo? result)
        {
            _result = result;
        }

        public Task<MediaInfo?> ProbeMediaAsync(object source, string[]? options = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
