using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
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
                        TrackList.Add(new SubtitleTrack(track));
                        SelectedIndex = Count - 1;
                        return;
                    }
                }
            });
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
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
