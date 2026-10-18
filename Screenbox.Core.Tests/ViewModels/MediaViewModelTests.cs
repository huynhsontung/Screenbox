using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.Tests.Helpers;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Tests.ViewModels;

public class MediaViewModelTests
{
    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_ShouldUpdateMediaInfoAndCaptions()
    {
        var vm = new MediaViewModel(
            new PlayerContext(),
            new TestPlayerService(),
            new Uri("file:///c:/music/track01.flac"));

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
        var parsedInfo = new MediaInfo(MediaPlaybackType.Music, parsedMusic, new VideoInfo())
        {
            Size = 42_000_000,
            DateModified = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero)
        };
        var probeService = new TestMediaProbeService(parsedInfo);

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(vm.DetailsLoaded).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Parsed Song");
        await Assert.That(vm.Caption).IsEqualTo("Parsed Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Parsed Artist – Parsed Album");
        await Assert.That(vm.Duration).IsEqualTo(TimeSpan.FromMinutes(4));
        await Assert.That(vm.TrackNumberText).IsEqualTo("3");
        await Assert.That(vm.MediaInfo.Size).IsEqualTo(42_000_000UL);
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaInfo));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.Duration));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.MediaType));
        await Assert.That(changedProperties).Contains(nameof(MediaViewModel.TrackNumberText));
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_ForVideo_ShouldUpdateVideoAndAudioProperties()
    {
        var uri = new Uri("file:///c:/videos/movie.mkv");
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
        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, probedMusic, probedVideo));
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), uri);

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(vm.DetailsLoaded).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(vm.Name).IsEqualTo("Probed Movie");
        await Assert.That(vm.MediaInfo.VideoProperties.Width).IsEqualTo(3840U);
        await Assert.That(vm.MediaInfo.VideoProperties.Height).IsEqualTo(2160U);
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(15000000U);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(448000U);
    }

    [Test]
    public async Task LoadDetailsAsync_WithStorageMediaProbeService_ShouldAcceptConcreteImplementation()
    {
        var probedMusic = new MusicInfo
        {
            Title = "Storage Song",
            Artist = "Storage Artist",
            Album = "Storage Album",
            Duration = TimeSpan.FromMinutes(3)
        };
        var probedInfo = new MediaInfo(MediaPlaybackType.Music, probedMusic, new VideoInfo())
        {
            Size = 10_000_000,
            DateModified = DateTimeOffset.UtcNow
        };
        var storageProber = new MockStorageMediaProbeService(probedInfo);
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), new Uri("file:///c:/music/test.mp3"));

        await vm.LoadDetailsAsync(storageProber);

        await Assert.That(storageProber.CallCount).IsEqualTo(1);
        await Assert.That(vm.DetailsLoaded).IsTrue();
        await Assert.That(vm.Name).IsEqualTo("Storage Song");
        await Assert.That(vm.Caption).IsEqualTo("Storage Artist");
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_WhenFieldsAlreadyPopulatedByStorage_ShouldOnlyUpdateEmptyFields()
    {
        var uri = new Uri("file:///c:/videos/movie.mp4");
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), uri);

        var storageVideo = new VideoInfo
        {
            Title = "Storage Movie Title",
            Subtitle = "Original Subtitle",
            Producers = "Storage Producer",
            Writers = "Storage Writer",
            Year = 2021,
            Duration = TimeSpan.FromMinutes(95),
            Bitrate = 5_000_000,
            Width = 1920,
            Height = 1080
        };
        var storageMusic = new MusicInfo
        {
            Bitrate = 320_000
        };
        var storageInfo = new MediaInfo(MediaPlaybackType.Video, storageMusic, storageVideo)
        {
            Size = 1_500_000_000,
            DateModified = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero)
        };
        await vm.LoadDetailsAsync(new MockStorageMediaProbeService(storageInfo));

        await Assert.That(vm.MediaInfo.VideoProperties.Title).IsEqualTo("Storage Movie Title");
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(5_000_000U);
        await Assert.That(vm.MediaInfo.VideoProperties.Producers).IsEqualTo("Storage Producer");
        await Assert.That(vm.MediaInfo.VideoProperties.Writers).IsEqualTo("Storage Writer");
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(320_000U);

        var vlcVideo = new VideoInfo
        {
            Title = string.Empty,
            Subtitle = string.Empty,
            Producers = string.Empty,
            Writers = string.Empty,
            Bitrate = 0,
            ShowName = "Sci-Fi Series",
            Season = "Season 1",
            Episode = "Episode 4"
        };
        var vlcMusic = new MusicInfo
        {
            Bitrate = 0
        };
        var vlcInfo = new MediaInfo(MediaPlaybackType.Video, vlcMusic, vlcVideo);

        await vm.LoadDetailsAsync(new TestMediaProbeService(vlcInfo));

        await Assert.That(vm.MediaInfo.VideoProperties.Title).IsEqualTo("Storage Movie Title");
        await Assert.That(vm.MediaInfo.VideoProperties.Subtitle).IsEqualTo("Original Subtitle");
        await Assert.That(vm.MediaInfo.VideoProperties.Producers).IsEqualTo("Storage Producer");
        await Assert.That(vm.MediaInfo.VideoProperties.Writers).IsEqualTo("Storage Writer");
        await Assert.That(vm.MediaInfo.VideoProperties.Year).IsEqualTo(2021U);
        await Assert.That(vm.MediaInfo.VideoProperties.Duration).IsEqualTo(TimeSpan.FromMinutes(95));
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(5_000_000U);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(320_000U);
        await Assert.That(vm.MediaInfo.Size).IsEqualTo(1_500_000_000UL);

        await Assert.That(vm.MediaInfo.VideoProperties.ShowName).IsEqualTo("Sci-Fi Series");
        await Assert.That(vm.MediaInfo.VideoProperties.Season).IsEqualTo("Season 1");
        await Assert.That(vm.MediaInfo.VideoProperties.Episode).IsEqualTo("Episode 4");
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_WhenStorageFieldIsZeroOrEmpty_ShouldUpdateFromVlcProbe()
    {
        var uri = new Uri("file:///c:/videos/clip.mkv");
        var vm = new MediaViewModel(new PlayerContext(), new TestPlayerService(), uri);

        var storageVideo = new VideoInfo
        {
            Title = "Storage MKV",
            Bitrate = 0,
            Width = 0,
            Height = 0
        };
        var storageInfo = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), storageVideo);
        await vm.LoadDetailsAsync(new MockStorageMediaProbeService(storageInfo));

        var vlcVideo = new VideoInfo
        {
            Title = string.Empty,
            Bitrate = 4_500_000,
            Width = 1920,
            Height = 1080
        };
        var vlcMusic = new MusicInfo
        {
            Bitrate = 192_000
        };
        var vlcInfo = new MediaInfo(MediaPlaybackType.Video, vlcMusic, vlcVideo);
        await vm.LoadDetailsAsync(new TestMediaProbeService(vlcInfo));

        await Assert.That(vm.MediaInfo.VideoProperties.Title).IsEqualTo("Storage MKV");
        await Assert.That(vm.MediaInfo.VideoProperties.Bitrate).IsEqualTo(4_500_000U);
        await Assert.That(vm.MediaInfo.VideoProperties.Width).IsEqualTo(1920U);
        await Assert.That(vm.MediaInfo.VideoProperties.Height).IsEqualTo(1080U);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(192_000U);
    }

    private sealed class MockStorageMediaProbeService : StorageMediaProbeService
    {
        private readonly MediaInfo? _infoToReturn;
        public int CallCount { get; private set; }

        public MockStorageMediaProbeService(MediaInfo? infoToReturn) : base(Microsoft.Extensions.Logging.Abstractions.NullLogger<StorageMediaProbeService>.Instance)
        {
            _infoToReturn = infoToReturn;
        }

        public override Task<MediaInfo?> ProbeMediaAsync(object source, string[]? options = null, System.Threading.CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_infoToReturn);
        }
    }
}
