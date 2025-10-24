#nullable enable

using System;
using ProtoBuf;

namespace Screenbox.Core.Models;

[ProtoContract]
public class PersistentMediaRecord
{
    [ProtoMember(1)]
    public string Title { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Path { get; set; } = string.Empty;

    [ProtoMember(3)]
    public IMediaProperties Properties { get; set; } = new VideoInfo();

    [ProtoMember(4)]
    public DateTime DateAdded { get; set; } = DateTime.UtcNow; // Must be UTC

    public PersistentMediaRecord()
    {
        // Required for ProtoBuf deserialization
    }

    public PersistentMediaRecord(string title, string path, IMediaProperties properties, DateTimeOffset dateAdded)
    {
        Title = title;
        Path = path;
        Properties = properties;
        DateAdded = dateAdded.UtcDateTime;
    }
}
