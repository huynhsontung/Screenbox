using LibVLCSharp.Shared;
using System.Threading.Tasks;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
        internal string PendingTrackLabel { get; set; }

        private readonly Media _media;

        public PlaybackSubtitleTrackList(Media media)
        {
            _media = media;
            if (_media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(_media.Tracks);
            }
            else
            {
                _media.ParsedChanged += Media_ParsedChanged;
            }

            PendingTrackLabel = string.Empty;
        }

        internal async void NotifyTrackAdded(int trackId)
        {
            // Run in new thread due to VLC thread safety
            await Task.Run(() =>
            {
                foreach (MediaTrack track in _media.Tracks)
                {
                    if (track.TrackType == TrackType.Text && track.Id == trackId)
                    {
                        SubtitleTrack sub = new(track);
                        sub.Label ??= PendingTrackLabel;
                        PendingTrackLabel = string.Empty;
                        TrackList.Add(sub);
                        SelectedIndex = Count - 1;
                        return;
                    }
                }
            });
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            if (e.ParsedStatus != MediaParsedStatus.Done) return;
            _media.ParsedChanged -= Media_ParsedChanged;
            AddVlcMediaTracks(_media.Tracks);
        }

        private void AddVlcMediaTracks(MediaTrack[] tracks)
        {
            foreach (MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Text)
                {
                    TrackList.Add(new SubtitleTrack(track));
                }
            }
        }
    }
}
