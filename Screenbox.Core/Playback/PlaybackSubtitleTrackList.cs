using LibVLCSharp.Shared;
using System.Threading.Tasks;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
        // This only allows to add one external subtitle at a time
        // TODO: Find a better solution for pending subtitle track label
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

        internal async void NotifyTrackAdded(int trackId, MediaPlayer mediaPlayer)
        {
            // Delay to wait for _media.Tracks to populate
            // Run in new thread to ensure VLC thread safety
            await Task.Delay(50).ConfigureAwait(false);
            foreach (LibVLCSharp.Shared.MediaTrack track in _media.Tracks)
            {
                if (track.TrackType == TrackType.Text && track.Id == trackId)
                {
                    SubtitleTrack sub = new(track);
                    sub.Label ??= PendingTrackLabel;
                    PendingTrackLabel = string.Empty;
                    TrackList.Add(sub);
                    if (trackId == mediaPlayer.Spu)
                    {
                        SelectedIndex = Count - 1;
                    }
                    return;
                }
            }
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            if (e.ParsedStatus != MediaParsedStatus.Done) return;
            _media.ParsedChanged -= Media_ParsedChanged;
            AddVlcMediaTracks(_media.Tracks);
        }

        private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
        {
            foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Text)
                {
                    TrackList.Add(new SubtitleTrack(track));
                }
            }
        }
    }
}
