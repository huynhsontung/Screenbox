using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackVideoTrackList : SingleSelectTrackList<VideoTrack>
    {
        private readonly Media _media;

        public PlaybackVideoTrackList(Media media)
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

            SelectedIndex = 0;
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            AddVlcMediaTracks(_media.Tracks);
        }

        private void AddVlcMediaTracks(MediaTrack[] tracks)
        {
            foreach (MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Video)
                {
                    TrackList.Add(new VideoTrack(track));
                }
            }
        }
    }
}
