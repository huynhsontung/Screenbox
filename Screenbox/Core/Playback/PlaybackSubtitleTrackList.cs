using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback
{
    public class PlaybackSubtitleTrackList : ObservableTrackList<SubtitleTrack>
    {
        public PlaybackSubtitleTrackList(Media media)
        {
            if (media.IsParsed)
            {
                AddVlcMediaTracks(media.Tracks);
            }
            else
            {
                media.ParsedChanged += Media_ParsedChanged;
            }
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            Media media = (Media)sender;
            AddVlcMediaTracks(media.Tracks);
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
