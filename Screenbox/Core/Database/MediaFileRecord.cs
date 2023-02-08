#nullable enable

using System;
using Windows.Media;
using ProtoBuf;

namespace Screenbox.Core.Database
{
    [ProtoContract]
    internal record MediaFileRecord(string Path, string Name, MediaPlaybackType MediaType, string Album, string AlbumArtist, string[] Artists,
        TimeSpan Duration, uint TrackNumber, string Genre, uint Year)
    {
        [ProtoMember(1)] public string Path { get; set; } = Path;
        [ProtoMember(2)] public string Name { get; set; } = Name;
        [ProtoMember(3)] public MediaPlaybackType MediaType { get; set; } = MediaType;
        [ProtoMember(4)] public string Album { get; set; } = Album;
        [ProtoMember(5)] public string AlbumArtist { get; set; } = AlbumArtist;
        [ProtoMember(6)] public string[] Artists { get; set; } = Artists;
        [ProtoMember(7)] public TimeSpan Duration { get; set; } = Duration;
        [ProtoMember(8)] public uint TrackNumber { get; set; } = TrackNumber;
        [ProtoMember(9)] public string Genre { get; set; } = Genre;
        [ProtoMember(10)] public uint Year { get; set; } = Year;

        public MediaFileRecord() : this(string.Empty, string.Empty, MediaPlaybackType.Unknown, string.Empty,
            string.Empty, Array.Empty<string>(),
            TimeSpan.Zero, 0, string.Empty, 0)
        {
        }
    }
}
