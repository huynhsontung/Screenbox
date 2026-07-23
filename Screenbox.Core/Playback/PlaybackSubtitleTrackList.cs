#nullable enable

using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared;
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

        private int _delaySpu = -1;

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

        internal void SelectVlcSpu(int spu)
        {
            if (spu < 0)
            {
                SelectedIndex = -1;
                return;
            }

            // Spu may be set before tracks are populated. Delay select.
            if (Count == 0)
            {
                _delaySpu = spu;
                return;
            }

            for (int i = 0; i < Count; i++)
            {
                if (this[i].VlcSpu == spu)
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
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
            string fileName = file.Name;
            string filePath = file.Path;

            bool alreadyExists = _pendingSubtitleTracks.Any(x =>
                (!string.IsNullOrEmpty(filePath) && string.Equals(x.File.Path, filePath, System.StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(x.File.Name, fileName, System.StringComparison.OrdinalIgnoreCase)) ||
                TrackList.Any(t => string.Equals(t.Label, fileName, System.StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                if (select)
                {
                    int existingIndex = TrackList.FindIndex(t =>
                        string.Equals(t.Label, fileName, System.StringComparison.OrdinalIgnoreCase));
                    if (existingIndex >= 0)
                    {
                        SelectedIndex = existingIndex;
                    }
                }
                return;
            }

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
            if (_delaySpu >= 0)
                SelectVlcSpu(_delaySpu);
        }

        private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
        {
            foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Text)
                {
                    string trackDesc = track.Description ?? string.Empty;

                    var matchingLazy = _pendingSubtitleTracks.FirstOrDefault(x =>
                        x.Track.VlcSpu == -1 &&
                        (!string.IsNullOrEmpty(trackDesc) && (
                            string.Equals(x.File.Name, trackDesc, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x.Track.Label, trackDesc, System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(System.IO.Path.GetFileNameWithoutExtension(x.File.Name), trackDesc, System.StringComparison.OrdinalIgnoreCase) ||
                            x.File.Name.StartsWith(trackDesc, System.StringComparison.OrdinalIgnoreCase)
                        )));

                    if (matchingLazy != null)
                    {
                        matchingLazy.Track.VlcSpu = track.Id;
                        matchingLazy.Track.Id = track.Id.ToString();
                        continue;
                    }

                    bool exists = TrackList.Any(t => t.VlcSpu == track.Id ||
                        (!string.IsNullOrEmpty(trackDesc) && string.Equals(t.Label, trackDesc, System.StringComparison.OrdinalIgnoreCase)));

                    if (!exists)
                    {
                        TrackList.Add(new SubtitleTrack(track));
                    }
                }
            }
        }
    }
}
