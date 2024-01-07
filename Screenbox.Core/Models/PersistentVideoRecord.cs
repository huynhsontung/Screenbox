using ProtoBuf;

namespace Screenbox.Core.Models;

[ProtoContract]
internal class PersistentVideoRecord
{
    [ProtoMember(1)]
    public string Title { get; set; }

    [ProtoMember(2)]
    public string Path { get; set; }

    [ProtoMember(3)]
    public VideoInfo Properties { get; set; }

    public PersistentVideoRecord() { }

    public PersistentVideoRecord(string title, string path, VideoInfo properties)
    {
        Title = title;
        Path = path;
        Properties = properties;
    }
}
