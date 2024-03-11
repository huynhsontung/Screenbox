#nullable enable

using LibVLCSharp.Shared;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackVideoTrackList : SingleSelectTrackList<VideoTrack>
    {
        private readonly Media? _media;
        private readonly MediaPlaybackVideoTrackList? _source;

        public PlaybackVideoTrackList(Media media)
        {
            _media = media;
            if (media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(_media.Tracks);
            }
            else
            {
                media.ParsedChanged += Media_ParsedChanged;
            }

            SelectedIndex = 0;
        }

        public PlaybackVideoTrackList(MediaPlaybackVideoTrackList source)
        {
            _source = source;
            SelectedIndex = source.SelectedIndex;
            source.SelectedIndexChanged += (sender, args) => SelectedIndex = sender.SelectedIndex;
            foreach (Windows.Media.Core.VideoTrack videoTrack in source)
            {
                TrackList.Add(new VideoTrack(videoTrack));
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            // Only update for Windows track list. VLC track list is handled by the player.
            if (_source == null || _source.SelectedIndex == sender.SelectedIndex) return;
            _source.SelectedIndex = sender.SelectedIndex;
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            if (_media == null || e.ParsedStatus != MediaParsedStatus.Done) return;
            _media.ParsedChanged -= Media_ParsedChanged;
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
