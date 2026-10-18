using System;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;

namespace Screenbox.Core.Tests.Helpers;

public class MediaInfoExtensionsTests
{
    [Test]
    public async Task MergeSelective_WhenTargetIsEmpty_ShouldPopulateFromSource()
    {
        var target = new MediaInfo(MediaPlaybackType.Unknown);

        var sourceMusic = new MusicInfo
        {
            Title = "Source Song",
            Artist = "Source Artist",
            Album = "Source Album",
            AlbumArtist = "Source Album Artist",
            Composers = "Source Composer",
            Genre = "Rock",
            TrackNumber = 4,
            Year = 2024,
            Duration = TimeSpan.FromMinutes(3),
            Bitrate = 320_000
        };

        var sourceVideo = new VideoInfo
        {
            Title = "Source Video",
            Subtitle = "Source Subtitle",
            Producers = "Source Producer",
            Writers = "Source Writer",
            Duration = TimeSpan.FromMinutes(120),
            Year = 2023,
            Width = 1920,
            Height = 1080,
            Bitrate = 6_000_000,
            ShowName = "Source Show",
            Season = "Season 1",
            Episode = "Episode 2"
        };

        var source = new MediaInfo(MediaPlaybackType.Video, sourceMusic, sourceVideo)
        {
            Size = 2_000_000_000,
            DateModified = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero)
        };

        MediaInfo merged = target.MergeSelective(source);

        await Assert.That(merged.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(merged.Size).IsEqualTo(2_000_000_000UL);
        await Assert.That(merged.DateModified).IsEqualTo(new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero));

        await Assert.That(merged.MusicProperties.Title).IsEqualTo("Source Song");
        await Assert.That(merged.MusicProperties.Artist).IsEqualTo("Source Artist");
        await Assert.That(merged.MusicProperties.Album).IsEqualTo("Source Album");
        await Assert.That(merged.MusicProperties.AlbumArtist).IsEqualTo("Source Album Artist");
        await Assert.That(merged.MusicProperties.Composers).IsEqualTo("Source Composer");
        await Assert.That(merged.MusicProperties.Genre).IsEqualTo("Rock");
        await Assert.That(merged.MusicProperties.TrackNumber).IsEqualTo(4U);
        await Assert.That(merged.MusicProperties.Year).IsEqualTo(2024U);
        await Assert.That(merged.MusicProperties.Duration).IsEqualTo(TimeSpan.FromMinutes(3));
        await Assert.That(merged.MusicProperties.Bitrate).IsEqualTo(320_000U);

        await Assert.That(merged.VideoProperties.Title).IsEqualTo("Source Video");
        await Assert.That(merged.VideoProperties.Subtitle).IsEqualTo("Source Subtitle");
        await Assert.That(merged.VideoProperties.Producers).IsEqualTo("Source Producer");
        await Assert.That(merged.VideoProperties.Writers).IsEqualTo("Source Writer");
        await Assert.That(merged.VideoProperties.Duration).IsEqualTo(TimeSpan.FromMinutes(120));
        await Assert.That(merged.VideoProperties.Year).IsEqualTo(2023U);
        await Assert.That(merged.VideoProperties.Width).IsEqualTo(1920U);
        await Assert.That(merged.VideoProperties.Height).IsEqualTo(1080U);
        await Assert.That(merged.VideoProperties.Bitrate).IsEqualTo(6_000_000U);
        await Assert.That(merged.VideoProperties.ShowName).IsEqualTo("Source Show");
        await Assert.That(merged.VideoProperties.Season).IsEqualTo("Season 1");
        await Assert.That(merged.VideoProperties.Episode).IsEqualTo("Episode 2");
    }

    [Test]
    public async Task MergeSelective_WhenTargetIsPopulated_ShouldPreservePopulatedPropertiesAndNotResetToEmpty()
    {
        var targetMusic = new MusicInfo
        {
            Title = "Target Song",
            Artist = "Target Artist",
            Album = "Target Album",
            Bitrate = 256_000
        };

        var targetVideo = new VideoInfo
        {
            Title = "Target Movie",
            Producers = "Target Producer",
            Writers = "Target Writer",
            Bitrate = 5_000_000,
            Width = 1920,
            Height = 1080
        };

        var target = new MediaInfo(MediaPlaybackType.Video, targetMusic, targetVideo)
        {
            Size = 1_000_000_000,
            DateModified = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var sourceMusic = new MusicInfo
        {
            Title = string.Empty,
            Artist = string.Empty,
            Album = string.Empty,
            Bitrate = 0
        };

        var sourceVideo = new VideoInfo
        {
            Title = string.Empty,
            Producers = string.Empty,
            Writers = string.Empty,
            Bitrate = 0,
            Width = 0,
            Height = 0
        };

        var source = new MediaInfo(MediaPlaybackType.Unknown, sourceMusic, sourceVideo)
        {
            Size = 0,
            DateModified = default
        };

        MediaInfo merged = target.MergeSelective(source);

        await Assert.That(merged.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(merged.Size).IsEqualTo(1_000_000_000UL);
        await Assert.That(merged.DateModified).IsEqualTo(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await Assert.That(merged.MusicProperties.Title).IsEqualTo("Target Song");
        await Assert.That(merged.MusicProperties.Artist).IsEqualTo("Target Artist");
        await Assert.That(merged.MusicProperties.Album).IsEqualTo("Target Album");
        await Assert.That(merged.MusicProperties.Bitrate).IsEqualTo(256_000U);
        await Assert.That(merged.VideoProperties.Title).IsEqualTo("Target Movie");
        await Assert.That(merged.VideoProperties.Producers).IsEqualTo("Target Producer");
        await Assert.That(merged.VideoProperties.Writers).IsEqualTo("Target Writer");
        await Assert.That(merged.VideoProperties.Bitrate).IsEqualTo(5_000_000U);
        await Assert.That(merged.VideoProperties.Width).IsEqualTo(1920U);
        await Assert.That(merged.VideoProperties.Height).IsEqualTo(1080U);
    }

    [Test]
    public async Task MergeSelective_WhenTargetIsPartiallyPopulated_ShouldUpdateOnlyEmptyFields()
    {
        var targetVideo = new VideoInfo
        {
            Title = "Existing Movie",
            Bitrate = 0,
            Width = 0,
            Height = 0,
            ShowName = string.Empty
        };

        var target = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), targetVideo);

        var sourceVideo = new VideoInfo
        {
            Title = "Probed Fallback Title",
            Bitrate = 4_000_000,
            Width = 3840,
            Height = 2160,
            ShowName = "Probed Show"
        };

        var source = new MediaInfo(MediaPlaybackType.Video, new MusicInfo(), sourceVideo);

        MediaInfo merged = target.MergeSelective(source);

        await Assert.That(merged.VideoProperties.Title).IsEqualTo("Existing Movie");
        await Assert.That(merged.VideoProperties.Bitrate).IsEqualTo(4_000_000U);
        await Assert.That(merged.VideoProperties.Width).IsEqualTo(3840U);
        await Assert.That(merged.VideoProperties.Height).IsEqualTo(2160U);
        await Assert.That(merged.VideoProperties.ShowName).IsEqualTo("Probed Show");
    }
}
