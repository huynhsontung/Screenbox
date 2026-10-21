using System;
using System.IO;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;

namespace Screenbox.Core.Tests.MediaSources;

public class MediaSourceTests
{
    private readonly MediaSourceFactory _factory = new();

    [Test]
    public async Task FileMediaSource_FromPath_PopulatesProperties()
    {
        string path = @"C:\Music\TestArtist\AwesomeSong.mp3";
        var source = _factory.Create(path);

        await Assert.That(source).IsTypeOf<FileMediaSource>();
        var fileSource = (IFileMediaSource)source;
        await Assert.That(fileSource.Path).IsEqualTo(path);
        await Assert.That(fileSource.Location).IsEqualTo(path);
        await Assert.That(fileSource.Name).IsEqualTo("AwesomeSong.mp3");
        await Assert.That(fileSource.FileType).IsEqualTo(".mp3");
        await Assert.That(fileSource.MediaType).IsEqualTo(MediaPlaybackType.Music);
        await Assert.That(fileSource.StorageFile).IsNull();
    }

    [Test]
    public async Task VideoFormats_IncludingTsAndM2ts_ClassifiedAsVideo()
    {
        var tsSource = _factory.Create(@"C:\Videos\recording.ts");
        await Assert.That(tsSource.MediaType).IsEqualTo(MediaPlaybackType.Video);

        var m2tsSource = _factory.Create(@"C:\Videos\bluray.m2ts");
        await Assert.That(m2tsSource.MediaType).IsEqualTo(MediaPlaybackType.Video);

        var mp4Source = _factory.Create(@"C:\Videos\clip.mp4");
        await Assert.That(mp4Source.MediaType).IsEqualTo(MediaPlaybackType.Video);
    }

    [Test]
    public async Task M3uFiles_CreatedAsExpandableMediaSource()
    {
        var m3uSource = _factory.Create(@"C:\Playlists\favorites.m3u");
        await Assert.That(m3uSource).IsAssignableTo<IExpandableMediaSource>();
        await Assert.That(m3uSource).IsAssignableTo<IFileMediaSource>();
        await Assert.That(m3uSource.MediaType).IsEqualTo(MediaPlaybackType.Playlist);

        var m3u8Source = _factory.Create(@"C:\Playlists\favorites.m3u8");
        await Assert.That(m3u8Source).IsAssignableTo<IExpandableMediaSource>();
    }

    [Test]
    public async Task UriMediaSource_FromNetworkUri_PopulatesProperties()
    {
        var uri = new Uri("https://stream.example.com/live/audio.aac");
        var source = _factory.Create(uri);

        await Assert.That(source).IsTypeOf<UriMediaSource>();
        var uriSource = (IUriMediaSource)source;
        await Assert.That(uriSource.Uri).IsEqualTo(uri);
        await Assert.That(uriSource.Location).IsEqualTo("https://stream.example.com/live/audio.aac");
        await Assert.That(uriSource.Name).IsEqualTo("audio.aac");
    }

    [Test]
    public async Task FileUri_NormalizedToFileMediaSource()
    {
        var fileUri = new Uri("file:///C:/Media/Track01.flac");
        var source = _factory.Create(fileUri);

        await Assert.That(source).IsAssignableTo<IFileMediaSource>();
        var fileSource = (IFileMediaSource)source;
        await Assert.That(fileSource.Path).IsEqualTo(@"C:\Media\Track01.flac");
        await Assert.That(fileSource.Location).IsEqualTo(@"C:\Media\Track01.flac");
        await Assert.That(fileSource.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task FilesHelpers_ExtensionMethods_MatchCorrectly()
    {
        await Assert.That(FilesHelpers.IsSupportedAudio(".flac")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedAudio(".mp3")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedAudio(".mp4")).IsFalse();

        await Assert.That(FilesHelpers.IsSupportedVideo(".m2ts")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedVideo(".ts")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedVideo(".mkv")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedVideo(".mp3")).IsFalse();

        await Assert.That(FilesHelpers.IsSupportedPlaylist(".m3u")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedPlaylist(".m3u8")).IsTrue();
        await Assert.That(FilesHelpers.IsSupportedPlaylist(".ts")).IsFalse();
    }
}
