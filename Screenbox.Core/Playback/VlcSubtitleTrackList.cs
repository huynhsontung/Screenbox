using Screenbox.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using LibVLCSharp.Shared;
using Windows.Storage;

namespace Screenbox.Core.Playback;

public sealed partial class VlcSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>, ISubtitleTrackList
{
    private readonly Media _media;
    private readonly List<LazySubtitleTrack> _pendingSubtitleTracks;

    internal VlcMediaPlayer? Player { get; set; }

    private class LazySubtitleTrack
    {
        public SubtitleTrack Track { get; }

        public StorageFile File { get; }

        public LazySubtitleTrack(StorageFile file)
        {
            File = file;
            Track = new SubtitleTrack("-1", file.Name);
        }
    }

    private int _delaySpu = -1;

    public VlcSubtitleTrackList(Media media)
    {
        _pendingSubtitleTracks = new List<LazySubtitleTrack>();
        _media = media;
        var tracks = _media.Tracks;
        if (tracks.Length > 0)
        {
            AddVlcMediaTracks(tracks);
        }
        else
        {
            _media.ParsedChanged += Media_ParsedChanged;
        }

        SelectedIndexChanged += OnSelectedIndexChanged;
    }

    internal void Detach()
    {
        _media.ParsedChanged -= Media_ParsedChanged;
        Player = null;
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

        string spuStr = spu.ToString();
        for (int i = 0; i < Count; i++)
        {
            if (this[i].Id == spuStr)
            {
                SelectedIndex = i;
                break;
            }
        }
    }

    private void OnSelectedIndexChanged(Windows.Media.Core.ISingleSelectMediaTrackList sender, object? args)
    {
        if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { } selectedTrack &&
            _pendingSubtitleTracks.FirstOrDefault(x => ReferenceEquals(x.Track, selectedTrack)) is { } lazyTrack)
        {
            int trackSpu = int.TryParse(selectedTrack.Id, out int s) ? s : -1;
            if (Player != null && (trackSpu == -1 || Player.VlcPlayer.SpuCount < trackSpu))
            {
                selectedTrack.Id = "-1";
                Player.AddSubtitle(lazyTrack.File, true);
            }
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public void AddExternalSubtitle(IStorageFile file, bool select = true)
    {
        if (file is StorageFile storageFile)
        {
            AddExternalSubtitleInternal(storageFile, select);
        }
    }

    private void AddExternalSubtitleInternal(StorageFile file, bool select)
    {
        string filePath = file.Path;

        // Check if the subtitle track already exists in the pending list
        var existing = _pendingSubtitleTracks.FirstOrDefault(x =>
            !string.IsNullOrEmpty(filePath) &&
            string.Equals(x.File.Path, filePath, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            if (select)
            {
                int existingIndex = TrackList.FindIndex(t => ReferenceEquals(t, existing.Track));
                if (existingIndex >= 0)
                {
                    SelectedIndex = existingIndex;
                }
            }

            return;
        }

        var lazySub = new LazySubtitleTrack(file);
        _pendingSubtitleTracks.Add(lazySub);
        TrackList.Add(lazySub.Track);

        if (select)
        {
            SelectedIndex = TrackList.Count - 1;
        }
    }

    internal void NotifyTrackAdded(int trackId)
    {
        if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { } selectedTrack && selectedTrack.Id == "-1")
        {
            selectedTrack.Id = trackId.ToString();
        }
    }

    private void Media_ParsedChanged(object? sender, MediaParsedChangedEventArgs e)
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
                TrackList.Add(new SubtitleTrack(
                    track.Id.ToString(),
                    track.Description,
                    track.Language ?? string.Empty));
            }
        }
    }
}
