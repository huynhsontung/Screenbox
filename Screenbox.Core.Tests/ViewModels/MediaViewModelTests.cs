using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
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
        var source = new UriMediaSource(new Uri("https://example.com/stream.ogg"));
        var vm = new MediaViewModel(source);

        var probedMusic = new MusicInfo
        {
            Title = "Vorbis Track",
            Artist = "Vorbis Artist",
            Album = "Vorbis Album",
            AlbumArtist = "Vorbis Album Artist",
            Genre = "Indie",
            TrackNumber = 3,
            Year = 2023,
            Duration = TimeSpan.FromMinutes(4),
            Bitrate = 192000
        };
        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Music, probedMusic, new VideoInfo()));

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(vm.DetailsLoaded).IsTrue();
        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Vorbis Track");
        await Assert.That(vm.Caption).IsEqualTo("Vorbis Artist");
        await Assert.That(vm.AltCaption).IsEqualTo("Vorbis Artist – Vorbis Album");
        await Assert.That(vm.TrackNumberText).IsEqualTo("3");
        await Assert.That(vm.Duration).IsEqualTo(TimeSpan.FromMinutes(4));
        await Assert.That(vm.MediaInfo.MusicProperties.AlbumArtist).IsEqualTo("Vorbis Album Artist");
        await Assert.That(vm.MediaInfo.MusicProperties.Genre).IsEqualTo("Indie");
        await Assert.That(vm.MediaInfo.MusicProperties.Year).IsEqualTo(2023u);
        await Assert.That(vm.MediaInfo.MusicProperties.Bitrate).IsEqualTo(192000u);
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_OnFileSource_ShouldNotMarkDetailsLoadedUntilStorageProbed()
    {
        var source = new FileMediaSource(@"C:\videos\movie.mkv");
        var vm = new MediaViewModel(source);

        var vlcVideo = new VideoInfo
        {
            Width = 1920,
            Height = 1080
        };
        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), vlcVideo));

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(vm.DetailsLoaded).IsFalse();
        await Assert.That(vm.MediaInfo.VideoProperties.Width).IsEqualTo(1920u);

        var storageProbeService = new StorageMediaProbeService(NullLogger<StorageMediaProbeService>.Instance);
        await vm.LoadDetailsAsync(storageProbeService);

        await Assert.That(vm.DetailsLoaded).IsTrue();
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_ShouldPassMediaOptions()
    {
        var source = new UriMediaSource(new Uri("https://example.com/live.m3u8"));
        var vm = new MediaViewModel(source);
        vm.SetOptions(":network-caching=1000 :http-user-agent=Screenbox");

        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video));

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(probeService.CallCount).IsEqualTo(1);
        await Assert.That(probeService.LastOptions).IsNotNull();
        await Assert.That(probeService.LastOptions!.Length).IsEqualTo(2);
        await Assert.That(probeService.LastOptions[0]).IsEqualTo(":network-caching=1000");
        await Assert.That(probeService.LastOptions[1]).IsEqualTo(":http-user-agent=Screenbox");
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_ShouldPreserveKnownMediaType()
    {
        var source = new FileMediaSource(@"C:\music\song.mp3");
        var vm = new MediaViewModel(source)
        {
            MediaInfo = new MediaInfo(MediaPlaybackType.Music)
        };

        var probeService = new TestMediaProbeService(new MediaInfo(MediaPlaybackType.Video, title: "Embedded Cover Track"));

        await vm.LoadDetailsAsync(probeService);

        await Assert.That(vm.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(vm.Name).IsEqualTo("Embedded Cover Track");
    }

    [Test]
    public async Task LoadDetailsAsync_WithMediaProbeService_ShouldNotResetExistingStorageProperties()
    {
        var source = new FileMediaSource(@"C:\videos\movie.mp4");
        var vm = new MediaViewModel(source);

        var storageVideo = new VideoInfo
        {
            Title = "Storage Movie Title",
            Subtitle = "Storage Subtitle",
            Producers = "Storage Producer",
            Writers = "Storage Writer",
            Year = 2021,
            Duration = TimeSpan.FromMinutes(95),
            Width = 1920,
            Height = 1080,
            Bitrate = 5_000_000
        };
        var storageMusic = new MusicInfo
        {
            Bitrate = 320_000
        };
        var storageInfo = new MediaInfo(MediaPlaybackType.Video, storageMusic, storageVideo)
        {
            Size = 1_500_000_000,
            DateModified = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero)
        };
        await vm.LoadDetailsAsync(new TestMediaProbeService(storageInfo));

        var vlcVideo = new VideoInfo
        {
            Title = string.Empty,
            Subtitle = string.Empty,
            Producers = string.Empty,
            Writers = string.Empty,
            Year = 0,
            Duration = TimeSpan.Zero,
            Width = 1920,
            Height = 1080,
            Bitrate = 0,
            ShowName = "Sci-Fi Series",
            Season = "Season 1",
            Episode = "Episode 4"
        };
        var vlcInfo = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), vlcVideo);
        await vm.LoadDetailsAsync(new TestMediaProbeService(vlcInfo));

        await Assert.That(vm.Name).IsEqualTo("Storage Movie Title");
        await Assert.That(vm.MediaInfo.VideoProperties.Title).IsEqualTo("Storage Movie Title");
        await Assert.That(vm.MediaInfo.VideoProperties.Subtitle).IsEqualTo("Storage Subtitle");
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
        var source = new FileMediaSource(@"C:\videos\clip.mkv");
        var vm = new MediaViewModel(source);

        var storageVideo = new VideoInfo
        {
            Title = "Storage MKV",
            Bitrate = 0,
            Width = 0,
            Height = 0
        };
        var storageInfo = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), storageVideo);
        await vm.LoadDetailsAsync(new TestMediaProbeService(storageInfo));

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
}
