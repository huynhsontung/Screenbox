#nullable enable

using System;
using System.Text.Json.Serialization;
using ProtoBuf;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

[ProtoContract]
public class PersistentMediaRecord
{
    [ProtoMember(1)]
    public string Title { get; set; }

    [ProtoMember(2)]
    public string Path { get; set; }

    [JsonIgnore]
    [ProtoMember(3)]
    public IMediaProperties? Properties { get; set; }

    [ProtoMember(4)]
    public DateTime DateAdded { get; set; } // Must be UTC

    [ProtoMember(5)]
    public TimeSpan Duration { get; set; }

    [ProtoMember(6)]
    public uint Year { get; set; }

    [ProtoMember(7)]
    public MediaPlaybackType MediaType { get; set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public PersistentMediaRecord()
    {
        // Required for ProtoBuf deserialization
        // Properties must be uninitialized or there will be a stack overflow exception
    }

    public PersistentMediaRecord(string title, string path, IMediaProperties properties, DateTimeOffset dateAdded)
    {
        Title = title;
        Path = path;
        DateAdded = dateAdded.UtcDateTime;
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
