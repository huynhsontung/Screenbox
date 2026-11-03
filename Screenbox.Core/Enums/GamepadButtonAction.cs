namespace Screenbox.Core.Enums;

/// <summary>
/// Represents the action performed when a gamepad button is pressed while
/// the player is visible.
/// </summary>
/// <remarks>
/// Values are persisted in the user settings. Existing members must keep their
/// numeric value so that previously saved mappings stay valid.
/// </remarks>
public enum GamepadButtonAction
{
    None = 0,
    ToggleFullscreen = 1,
    OpenPlayQueue = 2,
    OpenSubtitleMenu = 3,
    PlayPause = 4,
    TogglePlayerVisibility = 5,
    OpenPlaybackOptions = 6,
    Rewind = 7,
    FastForward = 8,
    RewindLong = 9,
    FastForwardLong = 10,
    IncreaseVolume = 11,
    DecreaseVolume = 12,
    PreviousTrack = 13,
    NextTrack = 14,
}
