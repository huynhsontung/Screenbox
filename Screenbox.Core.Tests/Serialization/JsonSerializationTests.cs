#nullable enable

using System.Text.Json;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Models.Serialization;

namespace Screenbox.Core.Tests.Serialization;

public sealed class JsonSerializationTests
{
    [Test]
    public async Task PlaylistRecordDto_SerializationAndDeserialization_UsingCoreJsonContext()
    {
        var original = new PlaylistRecordDto
        {
            Id = "pl_json_test_01",
            DisplayName = "Synthwave Beats",
            LastUpdated = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero),
            Items =
            [
                new RawMediaRecordDto
                {
                    Path = @"C:\Music\Synth\track1.flac",
                    Title = "Midnight City",
                    Artist = "M83",
                    Album = "Hurry Up, We're Dreaming",
                    MediaType = MediaPlaybackType.Music,
                    Duration = TimeSpan.FromMinutes(4.0),
                    Bitrate = 960000,
                    TrackNumber = 1
                }
            ]
        };

        string json = JsonSerializer.Serialize(original, CoreJsonContext.Default.PlaylistRecordDto);
        await Assert.That(string.IsNullOrWhiteSpace(json)).IsFalse();

        PlaylistRecordDto? deserialized = JsonSerializer.Deserialize(json, CoreJsonContext.Default.PlaylistRecordDto);

        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized.Id).IsEqualTo(original.Id);
        await Assert.That(deserialized.DisplayName).IsEqualTo(original.DisplayName);
        await Assert.That(deserialized.LastUpdated).IsEqualTo(original.LastUpdated);
        await Assert.That(deserialized.Items).HasSingleItem();

        RawMediaRecordDto item = deserialized.Items[0];
        await Assert.That(item.Path).IsEqualTo(@"C:\Music\Synth\track1.flac");
        await Assert.That(item.Title).IsEqualTo("Midnight City");
        await Assert.That(item.Artist).IsEqualTo("M83");
        await Assert.That(item.MediaType).IsEqualTo(MediaPlaybackType.Music);
    }

    [Test]
    public async Task RawMediaRecordDto_SerializationAndDeserialization_HandlesAllProperties()
    {
        var original = new RawMediaRecordDto
        {
            Path = @"C:\Videos\demo.mp4",
            Title = "Demo Video",
            MediaType = MediaPlaybackType.Video,
            DateAdded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMinutes(10),
            Year = 2024,
            Artist = "Director A",
            Album = "Short Films",
            AlbumArtist = "Studio B",
            Composers = "Composer C",
            Genre = "Documentary",
            TrackNumber = 5,
            Bitrate = 4000000,
            Subtitle = "English",
            Producers = "Producer D",
            Writers = "Writer E",
            Width = 3840,
            Height = 2160,
            VideoBitrate = 15000000
        };

        string json = JsonSerializer.Serialize(original, CoreJsonContext.Default.RawMediaRecordDto);
        RawMediaRecordDto? deserialized = JsonSerializer.Deserialize(json, CoreJsonContext.Default.RawMediaRecordDto);

        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized.Path).IsEqualTo(original.Path);
        await Assert.That(deserialized.Title).IsEqualTo(original.Title);
        await Assert.That(deserialized.Width).IsEqualTo(original.Width);
        await Assert.That(deserialized.Height).IsEqualTo(original.Height);
        await Assert.That(deserialized.VideoBitrate).IsEqualTo(original.VideoBitrate);
    }
}
