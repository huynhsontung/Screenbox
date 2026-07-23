#nullable enable

using System.Text.Json;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Models.Serialization;
using Xunit;

namespace Screenbox.Core.Tests.Serialization;

public sealed class JsonSerializationTests
{
    [Fact]
    public void PlaylistRecordDto_SerializationAndDeserialization_UsingCoreJsonContext()
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
        Assert.False(string.IsNullOrWhiteSpace(json));

        PlaylistRecordDto? deserialized = JsonSerializer.Deserialize(json, CoreJsonContext.Default.PlaylistRecordDto);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.DisplayName, deserialized.DisplayName);
        Assert.Equal(original.LastUpdated, deserialized.LastUpdated);
        Assert.Single(deserialized.Items);

        RawMediaRecordDto item = deserialized.Items[0];
        Assert.Equal(@"C:\Music\Synth\track1.flac", item.Path);
        Assert.Equal("Midnight City", item.Title);
        Assert.Equal("M83", item.Artist);
        Assert.Equal(MediaPlaybackType.Music, item.MediaType);
    }

    [Fact]
    public void RawMediaRecordDto_SerializationAndDeserialization_HandlesAllProperties()
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

        Assert.NotNull(deserialized);
        Assert.Equal(original.Path, deserialized.Path);
        Assert.Equal(original.Title, deserialized.Title);
        Assert.Equal(original.Width, deserialized.Width);
        Assert.Equal(original.Height, deserialized.Height);
        Assert.Equal(original.VideoBitrate, deserialized.VideoBitrate);
    }
}
