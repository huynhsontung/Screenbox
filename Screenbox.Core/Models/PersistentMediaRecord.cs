#nullable enable

using System;
using ProtoBuf;

namespace Screenbox.Core.Models;

[ProtoContract]
public class PersistentMediaRecord
{
    [ProtoMember(1)]
    public string Title { get; set; }

    [ProtoMember(2)]
    public string Path { get; set; }

    [ProtoMember(3)]
    public IMediaProperties Properties { get; set; }

    [ProtoMember(4)]
    public DateTime DateAdded { get; set; } // Must be UTC

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
        Properties = properties;
        DateAdded = dateAdded.UtcDateTime;
    }
}
