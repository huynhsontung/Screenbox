using ProtoBuf;
using System;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Models;

[ProtoContract]
public sealed class MusicInfo : IMediaProperties
{
    [ProtoMember(1)] public string Title { get; set; } = string.Empty;
    [ProtoMember(2)] public string Artist { get; set; } = string.Empty;
    [ProtoMember(3)] public string Album { get; set; } = string.Empty;
    [ProtoMember(4)] public string AlbumArtist { get; set; } = string.Empty;
    [ProtoMember(5)] public string Composers { get; set; } = string.Empty;
    [ProtoMember(6)] public string Genre { get; set; } = string.Empty;
    [ProtoMember(7)] public uint TrackNumber { get; set; }
    [ProtoMember(8)] public uint Year { get; set; }
    [ProtoMember(9)] public TimeSpan Duration { get; set; }
    [ProtoMember(10)] public uint Bitrate { get; set; }

    public MusicInfo() { }

    public MusicInfo(MusicProperties musicProperties)
    {
        Title = musicProperties.Title;
        Artist = musicProperties.Artist;
        Album = musicProperties.Album;
        AlbumArtist = musicProperties.AlbumArtist;
        Composers = string.Join(", ", musicProperties.Composers);
        Genre = string.Join(", ", musicProperties.Genre);
        TrackNumber = musicProperties.TrackNumber;
        Year = musicProperties.Year;
        Duration = musicProperties.Duration;
        Bitrate = musicProperties.Bitrate;
    }
}