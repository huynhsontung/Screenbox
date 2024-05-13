using ProtoBuf;
using System.Collections.Generic;

namespace Screenbox.Core.Models;

[ProtoContract]
internal class PersistentVideoLibrary
{
    [ProtoMember(1)] public List<string> FolderPaths { get; set; } = new();

    [ProtoMember(2)] public List<PersistentVideoRecord> VideoRecords { get; set; } = new();
}
