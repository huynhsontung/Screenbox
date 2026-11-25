using Screenbox.Core.Services;
using Windows.Media;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for audio and media stream playback state, volume and volume boost,
/// repeat mode, subtitle language preferences, and playback position persistence and resume.
/// </summary>
public sealed partial class PlaybackSettingsContext : SettingsContextBase<PlaybackSettingsContext>
{
    private const string PersistentVolumeKey = "Values/Volume";
    private const string MaxVolumeKey = "Values/MaxVolume";
    private const string PersistentRepeatModeKey = "Values/RepeatMode";
    private const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";
    private const string PersistPlaybackPositionKey = "Privacy/PersistPlaybackPosition";
    private const string RestorePlaybackPositionKey = "General/RestorePlaybackPosition";
    private const string SubtitleTextScaleKey = "Subtitle/TextScale";
    private const string SubtitleMarginKey = "Subtitle/Margin";
    private const string SubtitleColorKey = "Subtitle/Color";
    private const string SubtitleOutlineThicknessKey = "Subtitle/OutlineThickness";
    private const string SubtitleBackgroundOpacityKey = "Subtitle/BackgroundOpacity";
    private const string SubtitleBackgroundColorKey = "Subtitle/BackgroundColor";

    public int Volume
    {
        get => Get(PersistentVolumeKey, 100);
        set => Set(PersistentVolumeKey, value);
    }

    public int MaxVolume
    {
        get => Get(MaxVolumeKey, 100);
        set => Set(MaxVolumeKey, value);
    }

    public MediaPlaybackAutoRepeatMode RepeatMode
    {
        get => Get(PersistentRepeatModeKey, MediaPlaybackAutoRepeatMode.None);
        set => Set(PersistentRepeatModeKey, value);
    }

    public string SubtitleLanguage
    {
        get => Get(PersistentSubtitleLanguageKey, string.Empty);
        set => Set(PersistentSubtitleLanguageKey, value);
    }

    public bool PersistPlaybackPosition
    {
        get => Get(PersistPlaybackPositionKey, true);
        set => Set(PersistPlaybackPositionKey, value);
    }

    public bool RestorePlaybackPosition
    {
        get => Get(RestorePlaybackPositionKey, false);
        set => Set(RestorePlaybackPositionKey, value);
    }

    public int SubtitleTextScale
    {
        get => Get(SubtitleTextScaleKey, 100);
        set => Set(SubtitleTextScaleKey, value);
    }

    public int SubtitleMargin
    {
        get => Get(SubtitleMarginKey, 0);
        set => Set(SubtitleMarginKey, value);
    }

    public int SubtitleColor
    {
        get => Get(SubtitleColorKey, 0x00FFFFFF);
        set => Set(SubtitleColorKey, value);
    }

    public int SubtitleOutlineThickness
    {
        get => Get(SubtitleOutlineThicknessKey, 4);
        set => Set(SubtitleOutlineThicknessKey, value);
    }

    public int SubtitleBackgroundOpacity
    {
        get => Get(SubtitleBackgroundOpacityKey, 0);
        set => Set(SubtitleBackgroundOpacityKey, value);
    }

    public int SubtitleBackgroundColor
    {
        get => Get(SubtitleBackgroundColorKey, 0x000000);
        set => Set(SubtitleBackgroundColorKey, value);
    }

    public PlaybackSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
