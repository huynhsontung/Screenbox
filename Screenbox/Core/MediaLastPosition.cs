using System;
using ProtoBuf;

namespace Screenbox.Core
{
    [ProtoContract]
    internal class MediaLastPosition
    {
        [ProtoMember(1)]
        public string Location { get; set; }
        [ProtoMember(2)]
        public TimeSpan Position { get; set; }

        public MediaLastPosition(string location, TimeSpan position)
        {
            Location = location;
            Position = position;
        }
    }
}
