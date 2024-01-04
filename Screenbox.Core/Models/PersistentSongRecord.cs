using ProtoBuf;

namespace Screenbox.Core.Models;

[ProtoContract]
internal record PersistentSongRecord
{
    [ProtoMember(1)]
    public string Id { get; set; }

    [ProtoMember(2)]
    public string Title { get; set; }

    [ProtoMember(3)]
    public string Path { get; set; }

    [ProtoMember(4)]
    public MusicInfo Properties { get; set; }

    public PersistentSongRecord() { }

    public PersistentSongRecord(string id, string title, string path, MusicInfo properties)
    {
        Id = id;
        Title = title;
        Path = path;
        Properties = properties;
    }
}
