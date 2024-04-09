#nullable enable

using LibVLCSharp.Shared;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
        // This only allows to add one external subtitle at a time
        // TODO: Find a better solution for pending subtitle track label
        internal string PendingTrackLabel { get; set; }

        private readonly Media? _media;
        private readonly MediaPlaybackTimedMetadataTrackList? _source;

        public PlaybackSubtitleTrackList(Media media)
        {
            _media = media;
            if (media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(media.Tracks);
            }
            else
            {
                media.ParsedChanged += Media_ParsedChanged;
            }

            PendingTrackLabel = string.Empty;
        }

        public PlaybackSubtitleTrackList(MediaPlaybackTimedMetadataTrackList source)
        {
            _source = source;
            foreach (TimedMetadataTrack metadataTrack in source)
            {
                if (metadataTrack.TimedMetadataKind is TimedMetadataKind.Caption or TimedMetadataKind.Subtitle
                    or TimedMetadataKind.ImageSubtitle)
                {
                    TrackList.Add(new SubtitleTrack(metadataTrack));
                }
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
            PendingTrackLabel = string.Empty;
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            // Only update for Windows track list. VLC track list is handled by the player.
            if (_source == null) return;
            if (sender.SelectedIndex == -1)
            {
                for (uint i = 0; i < _source.Count; i++)
                {
                    _source.SetPresentationMode(i, TimedMetadataTrackPresentationMode.Disabled);
                }
            }
            else
            {
                _source.SetPresentationMode((uint)sender.SelectedIndex, TimedMetadataTrackPresentationMode.PlatformPresented);
            }
        }

        internal async void NotifyTrackAdded(int trackId, LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            if (_media == null) return;

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
            if (_media == null || e.ParsedStatus != MediaParsedStatus.Done) return;
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
