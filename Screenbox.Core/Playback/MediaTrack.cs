#nullable enable

using LibVLCSharp.Shared;
using Screenbox.Core.Helpers;
using System;
using Windows.Globalization;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;
public abstract class MediaTrack : IMediaTrack
{
    public string Id { get; internal set; }

    public string Label { get; set; }

    public string Language => _language?.DisplayName ?? _languageStr;

    private readonly Language? _language;
    private readonly string _languageStr;

    public MediaTrackKind TrackKind { get; }

    internal MediaTrack(MediaTrackKind trackKind, string language = "")
    {
        TrackKind = trackKind;
        _languageStr = language;
        Id = string.Empty;
        Label = string.Empty;
    }

    protected MediaTrack(LibVLCSharp.Shared.MediaTrack track)
    {
        TrackKind = Convert(track.TrackType);
        _languageStr = track.Language ?? string.Empty;
        if (Windows.Globalization.Language.IsWellFormed(_languageStr))
        {
            if (LanguageHelper.TryConvertISO6392ToISO6391(_languageStr, out string bc47Tag))
                _languageStr = bc47Tag;
            _language = new Language(_languageStr);
        }

        Id = track.Id.ToString();
        Label = GetFullLabel(track.Description, Language);
    }

    protected MediaTrack(IMediaTrack track)
    {
        _languageStr = track.Language;
        if (Windows.Globalization.Language.IsWellFormed(_languageStr))
        {
            _language = new Language(_languageStr);
        }

        Id = track.Id;
        Label = GetFullLabel(track.Label, Language);
    }

    private static string GetFullLabel(string? label, string language)
    {
        if (string.IsNullOrEmpty(label))
        {
            label = language;
        }
        else if (!string.IsNullOrEmpty(language) && language != label)
        {
            label = $"{label} ({language})";
        }

        return label ?? string.Empty;
    }

    private static MediaTrackKind Convert(TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio => MediaTrackKind.Audio,
            TrackType.Video => MediaTrackKind.Video,
            TrackType.Text => MediaTrackKind.TimedMetadata,
            _ => throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null)
        };
    }
}
