using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for privacy, history, and playback position tracking settings.
/// </summary>
public sealed partial class PrivacySettingsContext : SettingsContextBase
{
    private const string ShowRecentKey = "General/ShowRecent";
    private const string PersistPlaybackPositionKey = "Privacy/PersistPlaybackPosition";
    private const string RestorePlaybackPositionKey = "General/RestorePlaybackPosition";

    public bool ShowRecent
    {
        get => Get(ShowRecentKey, true);
        set => Set(ShowRecentKey, value);
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

    public PrivacySettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
