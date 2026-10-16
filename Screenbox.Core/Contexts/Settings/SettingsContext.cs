namespace Screenbox.Core.Contexts;

/// <summary>
/// Aggregates all domain-specific settings contexts into a single unified entry point.
/// </summary>
public sealed class SettingsContext
{
    /// <summary>
    /// Gets the media player UI, transport controls, gestures, video upscaling, and LibVLC engine settings.
    /// </summary>
    public PlayerSettingsContext Player { get; }

    /// <summary>
    /// Gets the media library indexing, removable storage scanning, and collection sorting settings.
    /// </summary>
    public LibrarySettingsContext Library { get; }

    /// <summary>
    /// Gets the audio and media stream playback state, volume, repeat mode, subtitle language, and position tracking settings.
    /// </summary>
    public PlaybackSettingsContext Playback { get; }

    /// <summary>
    /// Gets the general application settings, shell appearance, recent media history, and catch-all preferences.
    /// </summary>
    public GeneralSettingsContext General { get; }

    public SettingsContext(
        PlayerSettingsContext player,
        LibrarySettingsContext library,
        PlaybackSettingsContext playback,
        GeneralSettingsContext general)
    {
        Player = player;
        Library = library;
        Playback = playback;
        General = general;
    }
}
