using System;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;

namespace Screenbox.Core.Tests.Models;

public class MediaInfoTests
{
    [Test]
    public async Task Merge_ShouldPreferNonEmptyAndNonZeroPreferredValues()
    {
        var baseMusic = new MusicInfo
        {
            Title = "WinRT Title",
            Artist = "WinRT Artist",
            Album = "WinRT Album",
            AlbumArtist = "WinRT Album Artist",
            Composers = "WinRT Composer",
            Genre = "Pop",
            TrackNumber = 1,
            Year = 2020,
            Duration = TimeSpan.FromSeconds(100),
            Bitrate = 128000
        };
        var baseVideo = new VideoInfo
        {
            Title = "WinRT Title",
            Subtitle = "WinRT Subtitle",
            Producers = "WinRT Producer",
            Writers = "WinRT Writer",
            Duration = TimeSpan.FromSeconds(100),
            Year = 2020,
            Width = 1280,
            Height = 720,
            Bitrate = 2000000
        };
        var baseInfo = new MediaInfo(
            MediaPlaybackType.Video,
            baseMusic,
            baseVideo,
            size: 1024,
            dateModified: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var preferredMusic = new MusicInfo
        {
            Title = "LibVLC Title",
            Artist = "LibVLC Artist",
            Album = "LibVLC Album",
            AlbumArtist = "LibVLC Album Artist",
            Genre = "Rock",
            TrackNumber = 5,
            Year = 2024,
            Duration = TimeSpan.FromSeconds(180),
            Bitrate = 320000
        };
        var preferredVideo = new VideoInfo
        {
            Title = "LibVLC Title",
            Subtitle = "LibVLC Description",
            Duration = TimeSpan.FromSeconds(180),
            Year = 2024,
            Width = 1920,
            Height = 1080,
            Bitrate = 5000000,
            ShowName = "Test Show",
            Season = "2",
            Episode = "7"
        };
        var preferredInfo = new MediaInfo(MediaPlaybackType.Video, preferredMusic, preferredVideo);

        var merged = MediaInfo.Merge(baseInfo, preferredInfo);

        await Assert.That(merged.MediaType).IsEqualTo(MediaPlaybackType.Video);
        await Assert.That(merged.Size).IsEqualTo(1024UL);
        await Assert.That(merged.DateModified).IsEqualTo(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await Assert.That(merged.MusicProperties.Title).IsEqualTo("LibVLC Title");
        await Assert.That(merged.MusicProperties.Artist).IsEqualTo("LibVLC Artist");
        await Assert.That(merged.MusicProperties.Album).IsEqualTo("LibVLC Album");
        await Assert.That(merged.MusicProperties.AlbumArtist).IsEqualTo("LibVLC Album Artist");
        await Assert.That(merged.MusicProperties.Composers).IsEqualTo("WinRT Composer");
        await Assert.That(merged.MusicProperties.Genre).IsEqualTo("Rock");
        await Assert.That(merged.MusicProperties.TrackNumber).IsEqualTo(5U);
        await Assert.That(merged.MusicProperties.Year).IsEqualTo(2024U);
        await Assert.That(merged.MusicProperties.Duration).IsEqualTo(TimeSpan.FromSeconds(180));
        await Assert.That(merged.MusicProperties.Bitrate).IsEqualTo(320000U);

        await Assert.That(merged.VideoProperties.Title).IsEqualTo("LibVLC Title");
        await Assert.That(merged.VideoProperties.Subtitle).IsEqualTo("LibVLC Description");
        await Assert.That(merged.VideoProperties.Producers).IsEqualTo("WinRT Producer");
        await Assert.That(merged.VideoProperties.Writers).IsEqualTo("WinRT Writer");
        await Assert.That(merged.VideoProperties.Duration).IsEqualTo(TimeSpan.FromSeconds(180));
        await Assert.That(merged.VideoProperties.Year).IsEqualTo(2024U);
        await Assert.That(merged.VideoProperties.Width).IsEqualTo(1920U);
        await Assert.That(merged.VideoProperties.Height).IsEqualTo(1080U);
        await Assert.That(merged.VideoProperties.Bitrate).IsEqualTo(5000000U);
        await Assert.That(merged.VideoProperties.ShowName).IsEqualTo("Test Show");
        await Assert.That(merged.VideoProperties.Season).IsEqualTo("2");
        await Assert.That(merged.VideoProperties.Episode).IsEqualTo("7");
    }

    [Test]
    public async Task Merge_WhenBaseMediaTypeIsKnown_ShouldPreserveBaseMediaType()
    {
        var baseInfo = new MediaInfo(MediaPlaybackType.Music);
        var preferredInfo = new MediaInfo(MediaPlaybackType.Video);

        var merged = MediaInfo.Merge(baseInfo, preferredInfo);

        await Assert.That(merged.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task Merge_WhenBaseMediaTypeIsUnknown_ShouldAdoptPreferredMediaType()
    {
        var baseInfo = new MediaInfo(MediaPlaybackType.Unknown);
        var preferredInfo = new MediaInfo(MediaPlaybackType.Music);

        var merged = MediaInfo.Merge(baseInfo, preferredInfo);

        await Assert.That(merged.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task NormalizeTitle_ShouldFilterOutEmptyGuidAndDefaultSegmentName()
    {
        await Assert.That(VlcMediaExtensions.NormalizeTitle(null, "file:///C:/music/song.mp3")).IsEqualTo(string.Empty);
        await Assert.That(VlcMediaExtensions.NormalizeTitle("   ", "file:///C:/music/song.mp3")).IsEqualTo(string.Empty);
        await Assert.That(VlcMediaExtensions.NormalizeTitle(Guid.NewGuid().ToString(), "winrt://token")).IsEqualTo(string.Empty);
        await Assert.That(VlcMediaExtensions.NormalizeTitle("My Song.mp3", "file:///C:/music/My%20Song.mp3")).IsEqualTo(string.Empty);
        await Assert.That(VlcMediaExtensions.NormalizeTitle("Actual Song Title", "file:///C:/music/My%20Song.mp3")).IsEqualTo("Actual Song Title");
    }

    [Test]
    public async Task ParseTrackNumber_ShouldHandlePlainAndFractionFormats()
    {
        await Assert.That(VlcMediaExtensions.ParseTrackNumber(null)).IsEqualTo(0U);
        await Assert.That(VlcMediaExtensions.ParseTrackNumber("")).IsEqualTo(0U);
        await Assert.That(VlcMediaExtensions.ParseTrackNumber("5")).IsEqualTo(5U);
        await Assert.That(VlcMediaExtensions.ParseTrackNumber("05/12")).IsEqualTo(5U);
        await Assert.That(VlcMediaExtensions.ParseTrackNumber(" 14 / 20 ")).IsEqualTo(14U);
        await Assert.That(VlcMediaExtensions.ParseTrackNumber("invalid")).IsEqualTo(0U);
    }

    [Test]
    public async Task ParseYear_ShouldHandleYearAndDateFormats()
    {
        await Assert.That(VlcMediaExtensions.ParseYear(null)).IsEqualTo(0U);
        await Assert.That(VlcMediaExtensions.ParseYear("")).IsEqualTo(0U);
        await Assert.That(VlcMediaExtensions.ParseYear("2024")).IsEqualTo(2024U);
        await Assert.That(VlcMediaExtensions.ParseYear("2023-11-05")).IsEqualTo(2023U);
        await Assert.That(VlcMediaExtensions.ParseYear("1999-12-31T23:59:59Z")).IsEqualTo(1999U);
        await Assert.That(VlcMediaExtensions.ParseYear("unknown")).IsEqualTo(0U);
    }
}
