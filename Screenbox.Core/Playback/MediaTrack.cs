using Screenbox.Core.Helpers;
using Windows.Globalization;
using Windows.Media.Core;

namespace Screenbox.Core.Playback;

public abstract partial class MediaTrack : IMediaTrack
{
    public string Id { get; internal set; }

    public string Label { get; set; }

    public string Language => _language?.DisplayName ?? _languageStr;

    public string LanguageTag => _language?.LanguageTag ?? string.Empty;

    private readonly Language? _language;
    private readonly string _languageStr;

    public MediaTrackKind TrackKind { get; }

    protected MediaTrack(MediaTrackKind trackKind, string id, string? label, string language = "")
    {
        TrackKind = trackKind;
        Id = id ?? string.Empty;
        _languageStr = language ?? string.Empty;
        if (Windows.Globalization.Language.IsWellFormed(_languageStr))
        {
            if (LanguageHelper.TryConvertISO6392ToISO6391(_languageStr, out string bc47Tag))
                _languageStr = bc47Tag;
            _language = new Language(_languageStr);
        }

        Label = GetFullLabel(label, Language);
    }

    protected MediaTrack(IMediaTrack track)
    {
        TrackKind = track.TrackKind;
        _languageStr = track.Language ?? string.Empty;
        if (Windows.Globalization.Language.IsWellFormed(_languageStr))
        {
            _language = new Language(_languageStr);
        }

        Id = track.Id;
        Label = GetFullLabel(track.Label, Language);
    }

    internal static string GetFullLabel(string? label, string language)
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
}
