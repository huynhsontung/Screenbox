using ProtoBuf;
using System;

namespace Screenbox.Core.Models;

[ProtoContract]
[ProtoInclude(11, typeof(MusicInfo))]
[ProtoInclude(12, typeof(VideoInfo))]
internal interface IMediaProperties
{
    string Title { get; set; }
    uint Year { get; set; }
    TimeSpan Duration { get; set; }
}
