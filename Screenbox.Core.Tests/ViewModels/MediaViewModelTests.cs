using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
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
}
