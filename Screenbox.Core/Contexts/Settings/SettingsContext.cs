namespace Screenbox.Core.Contexts;

/// <summary>
/// Aggregates all domain-specific settings contexts into a single unified entry point.
/// </summary>
public sealed class SettingsContext
{
    public PlayerSettingsContext Player { get; }
    public LibrarySettingsContext Library { get; }
    public PlaybackSettingsContext Playback { get; }
    public GeneralSettingsContext General { get; }
    public AdvancedSettingsContext Advanced { get; }
    public PrivacySettingsContext Privacy { get; }
    public PersonalizationSettingsContext Personalization { get; }

    public SettingsContext(
        PlayerSettingsContext player,
        LibrarySettingsContext library,
        PlaybackSettingsContext playback,
        GeneralSettingsContext general,
        AdvancedSettingsContext advanced,
        PrivacySettingsContext privacy,
        PersonalizationSettingsContext personalization)
    {
        Player = player;
        Library = library;
        Playback = playback;
        General = general;
        Advanced = advanced;
        Privacy = privacy;
        Personalization = personalization;
    }
}
