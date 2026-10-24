using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback;

public sealed partial class VlcVideoTrackList : SingleSelectTrackList<VideoTrack>
{
    private readonly Media? _media;

    public VlcVideoTrackList(Media media)
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

    private void Media_ParsedChanged(object? sender, MediaParsedChangedEventArgs e)
    {
        if (_media == null || e.ParsedStatus != MediaParsedStatus.Done) return;
        _media.ParsedChanged -= Media_ParsedChanged;
        AddVlcMediaTracks(_media.Tracks);
    }

    private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
    {
        foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
        {
            if (track.TrackType == TrackType.Video)
            {
                TrackList.Add(new VideoTrack(
                    track.Id.ToString(),
                    track.Description ?? track.Language ?? track.Id.ToString(),
                    track.Description,
                    track.Language ?? string.Empty));
            }
        }
    }
}
