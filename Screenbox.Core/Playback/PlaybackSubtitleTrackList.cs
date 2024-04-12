using LibVLCSharp.Shared;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Core;
using Windows.Storage;

namespace Screenbox.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
        private readonly Media _media;
        private readonly List<LazySubtitleTrack> _pendingSubtitleTracks;

        private class LazySubtitleTrack
        {
            public SubtitleTrack Track { get; }

            public StorageFile File { get; }

            public VlcMediaPlayer Player { get; }

            public LazySubtitleTrack(VlcMediaPlayer player, StorageFile file)
            {
                Player = player;
                File = file;
                Track = new SubtitleTrack
                {
                    Id = "-1",
                    VlcSpu = -1,
                    Label = file.Name,
                };
            }
        }

        public PlaybackSubtitleTrackList(Media media)
        {
            _pendingSubtitleTracks = new List<LazySubtitleTrack>();
            _media = media;
            if (_media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(_media.Tracks);
            }
            else
            {
                _media.ParsedChanged += Media_ParsedChanged;
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object args)
        {
            if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { } selectedTrack &&
                _pendingSubtitleTracks.FirstOrDefault(x => ReferenceEquals(x.Track, selectedTrack)) is { } lazyTrack &&
                (selectedTrack.VlcSpu == -1 || lazyTrack.Player.VlcPlayer.SpuCount < selectedTrack.VlcSpu))
            {
                selectedTrack.VlcSpu = -1;
                lazyTrack.Player.AddSubtitle(lazyTrack.File, true);
            }
        }

        public void AddExternalSubtitle(VlcMediaPlayer player, StorageFile file, bool select)
        {
            var lazySub = new LazySubtitleTrack(player, file);
            _pendingSubtitleTracks.Add(lazySub);
            TrackList.Add(lazySub.Track);

            if (select)
            {
                SelectedIndex = TrackList.Count - 1;
            }
        }

        internal void NotifyTrackAdded(int trackId)
        {
            if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { VlcSpu: -1 } selectedTrack)
            {
                selectedTrack.VlcSpu = trackId;
                selectedTrack.Id = trackId.ToString();
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
