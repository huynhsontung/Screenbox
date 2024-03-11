#nullable enable

using CommunityToolkit.Diagnostics;
using LibVLCSharp.Shared;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public sealed class AudioTrack : IMediaTrack
    {
        internal int VlcTrackId { get; }

        public string Id { get; }

        public string Label { get; set; }

        public string Language { get; }

        public string Name { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.Audio;

        public AudioTrack(MediaTrack audioTrack)
        {
            Guard.IsTrue(audioTrack.TrackType == TrackType.Audio, nameof(audioTrack.TrackType));
            VlcTrackId = audioTrack.Id;
            Id = audioTrack.Id.ToString();
            Language = audioTrack.Language ?? string.Empty;
            Name = audioTrack.Description ?? audioTrack.Language ?? audioTrack.Id.ToString();
            Label = string.IsNullOrEmpty(audioTrack.Description)
                ? audioTrack.Language ?? string.Empty
                : $"{audioTrack.Description} ({audioTrack.Language})";
        }

        public AudioTrack(Windows.Media.Core.AudioTrack audioTrack)
        {
            Id = audioTrack.Id;
            Label = audioTrack.Label;
            Language = audioTrack.Language;
            Name = audioTrack.Name;
        }
    }
}
