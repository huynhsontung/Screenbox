using LibVLCSharp.Shared;

namespace Screenbox.Core.Playback;

public sealed partial class VlcAudioTrackList : SingleSelectTrackList<AudioTrack>
{
    private readonly Media _media;

    public VlcAudioTrackList(Media media)
    {
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

        SelectedIndex = 0;
    }

    internal void Detach()
    {
        _media.ParsedChanged -= Media_ParsedChanged;
    }

    private void Media_ParsedChanged(object? sender, MediaParsedChangedEventArgs e)
    {
        if (e.ParsedStatus != MediaParsedStatus.Done) return;
        _media.ParsedChanged -= Media_ParsedChanged;
        AddVlcMediaTracks(_media.Tracks);
    }

    private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
    {
        foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
        {
            if (track.TrackType == TrackType.Audio)
            {
                TrackList.Add(new AudioTrack(
                    track.Id.ToString(),
                    track.Description ?? track.Language ?? track.Id.ToString(),
                    track.Description,
                    track.Language ?? string.Empty));
            }
        }
    }
}
