using ProtoBuf;
using System;

namespace Screenbox.Core.Models
{
    [ProtoContract]
    internal record MediaPlaybackProgress(string Location, TimeSpan Position)
    {
        [ProtoMember(1)] public string Location { get; set; } = Location;
        [ProtoMember(2)] public TimeSpan Position { get; set; } = Position;

        public MediaPlaybackProgress() : this(string.Empty, TimeSpan.Zero)
        {
        }
    }
}
