using Screenbox.Core.Services;
using Windows.Media;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for playback preferences and audio levels.
/// </summary>
public sealed partial class PlaybackSettingsContext : SettingsContextBase
{
    private const string PersistentVolumeKey = "Values/Volume";
    private const string MaxVolumeKey = "Values/MaxVolume";
    private const string PersistentRepeatModeKey = "Values/RepeatMode";
    private const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";

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

    public PlaybackSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
