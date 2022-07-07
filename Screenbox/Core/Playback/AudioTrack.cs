#nullable enable

using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Windows.Media.Core;

namespace Screenbox.Core.Playback
{
    public class AudioTrack : IMediaTrack
    {
        public string Id { get; }

        public string? Label { get; set; }

        public string? Language { get; }
        
        public string Name { get; }

        public MediaTrackKind TrackKind => MediaTrackKind.Audio;

        //public Windows.Media.Core.AudioTrack? Source { get; }

        public AudioTrack(MediaTrack audioTrack)
        {
            Guard.IsTrue(audioTrack.TrackType == TrackType.Audio, nameof(audioTrack.TrackType));
            Id = audioTrack.Id.ToString();
            Language = Label = audioTrack.Language;
            Name = audioTrack.Language ?? audioTrack.Id.ToString();
        }

        //public AudioTrack(Windows.Media.Core.AudioTrack audioTrack)
        //{
        //    //Source = audioTrack;
        //    Id = audioTrack.Id;
        //    Label = audioTrack.Label;
        //    Language = audioTrack.Language;
        //    Name = audioTrack.Name;
        //}
    }
}
