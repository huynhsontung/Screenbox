namespace Screenbox.Core.Enums;

/// <summary>
/// Represents the action that was taken on the media playback.
/// </summary>
public enum PlaybackActionKind
{
    None,
    PlayPause,
    Rewind,
    FastForward,
    DecreaseVolume,
    IncreaseVolume,
    DecreaseRate,
    IncreaseRate,
    //PreviousTrack,
    //NextTrack,
    //PreviousChapter,
    //NextChapter,
    //DecreaseBrightness,
    //IncreaseBrightness,
}
