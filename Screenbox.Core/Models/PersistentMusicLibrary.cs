using ProtoBuf;
using System.Collections.Generic;

namespace Screenbox.Core.Models;

[ProtoContract]
internal class PersistentMusicLibrary
{
    [ProtoMember(1)] public List<string> FolderPaths { get; set; } = new();

    [ProtoMember(2)] public List<PersistentSongRecord> SongRecords { get; set; } = new();
}
