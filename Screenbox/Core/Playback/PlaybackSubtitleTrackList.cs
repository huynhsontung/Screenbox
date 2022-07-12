using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback
{
    public class PlaybackSubtitleTrackList : ObservableTrackList<SubtitleTrack>
    {
        private readonly Media _media;

        public PlaybackSubtitleTrackList(Media media)
        {
            _media = media;
            if (_media.IsParsed)
            {
                AddVlcMediaTracks(_media.Tracks);
            }
            else
            {
                _media.ParsedChanged += Media_ParsedChanged;
            }
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
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
