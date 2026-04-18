#nullable enable

using System;
using System.Text.Json.Serialization;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

public class PersistentMediaRecord
{
    public string Title { get; set; }

    public string Path { get; set; }

    // System.Text.Json in .NET Native does not support polymorphic type hierarchy serialization/deserialization
    [JsonIgnore]    // TODO: Remove this when we migrate to .NET AOT
    public IMediaProperties? Properties { get; set; }

    public DateTime DateAdded { get; set; } // Must be UTC

    public TimeSpan Duration { get; set; }

    public uint Year { get; set; }

    public MediaPlaybackType MediaType { get; set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public PersistentMediaRecord()
    {
    }
#pragma warning restore CS8618

    public PersistentMediaRecord(string title, string path, IMediaProperties properties, DateTimeOffset dateAdded)
    {
        Title = title;
        Path = path;
        DateAdded = dateAdded.UtcDateTime;
        Properties = properties;
        Duration = properties.Duration;
        Year = properties.Year;
        MediaType = properties switch
        {
            VideoInfo => MediaPlaybackType.Video,
            MusicInfo => MediaPlaybackType.Music,
            _ => MediaPlaybackType.Unknown,
        };
    }
}

