namespace Screenbox.Core.Enums;

/// <summary>
/// Defines constants that specify the playback action represented by an on-screen display update.
/// </summary>
public enum PlaybackCommandKind
{
    //None,
    Play,
    Pause,
    Stop,
    Rewind,
    FastForward,

    Next,
    Previous,
    NextChapter,
    PreviousChapter,

    VolumeUp,
    VolumeDown,
    Mute,

    RateUp,
    RateDown,

    AspectRatio,
    Scale,
    Subtitle,
    SubtitleOff,
}
