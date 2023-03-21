#nullable enable

using LibVLCSharp.Shared;
using CommunityToolkit.Diagnostics;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public sealed class AudioTrack : IMediaTrack
    {
        internal int VlcTrackId { get; }

        public string Id { get; }

        public string? Label { get; set; }

        public string? Language { get; }
        
        public string Name { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.Audio;

        public AudioTrack(MediaTrack audioTrack)
        {
            Guard.IsTrue(audioTrack.TrackType == TrackType.Audio, nameof(audioTrack.TrackType));
            VlcTrackId = audioTrack.Id;
            Id = audioTrack.Id.ToString();
            Language = audioTrack.Language;
            Name = audioTrack.Description ?? audioTrack.Language ?? audioTrack.Id.ToString();
            Label = string.IsNullOrEmpty(audioTrack.Description)
                ? audioTrack.Language
                : $"{audioTrack.Description} ({audioTrack.Language})";
        }
    }
}
