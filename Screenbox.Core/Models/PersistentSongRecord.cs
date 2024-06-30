using ProtoBuf;
using System;

namespace Screenbox.Core.Models;

[ProtoContract]
internal record PersistentSongRecord
{
    [ProtoMember(1)]
    public string Title { get; set; }

    [ProtoMember(2)]
    public string Path { get; set; }

    [ProtoMember(3)]
    public MusicInfo Properties { get; set; }

    [ProtoMember(4)]
    public DateTime DateAdded { get; set; } // Must be UTC datetime

    public PersistentSongRecord() { }

    public PersistentSongRecord(string title, string path, MusicInfo properties, DateTimeOffset dateAdded)
    {
        Title = title;
        Path = path;
        Properties = properties;
        DateAdded = dateAdded.UtcDateTime;
    }
}
