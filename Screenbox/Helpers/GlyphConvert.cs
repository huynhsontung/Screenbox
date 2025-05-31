using System;
using Windows.Media;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> methods to convert value types to glyph codes.
/// </summary>
public static partial class GlyphConvert
{
    /// <summary>
    /// Gets the shuffle glyph code based on a boolean condition.
    /// </summary>
    /// <remarks>The glyph code adapts according to the current text reading order.</remarks>
    /// <param name="value">A <see cref="bool"/> that specifies the shuffle mode.</param>
    /// <returns>
    /// <strong>Shuffle</strong> glyph code <see cref="string"/> if the <paramref name="value"/> is <see langword="true"/>;
    /// otherwise, <strong>Shuffle Off</strong> glyph code.
    /// </returns>
    public static string ToShuffleGlyph(bool value)
    {
        return value
            ? (GlobalizationHelper.IsRightToLeftLanguage ? "\U000F0021" : "\uE8B1")
            : (GlobalizationHelper.IsRightToLeftLanguage ? "\U000F002B" : "\U000F002A");
    }

    /// <summary>
    /// Gets the repeat glyph code based on the current auto-repeat mode of the player.
    /// </summary>
    /// <remarks>The glyph code adapts according to the current text reading order.</remarks>
    /// <param name="repeatMode">A <see langword="enum"/> that specifies the auto repeat mode for the <see cref="MediaPlaybackAutoRepeatMode"/>.</param>
    /// <returns>
    /// <strong>Repeat Off</strong> glyph code <see cref="string"/> if <paramref name="repeatMode"/> is <see cref="MediaPlaybackAutoRepeatMode.None"/>,
    /// <strong>Repeat All</strong> glyph code for <see cref="MediaPlaybackAutoRepeatMode.List"/>, or <strong>Repeat One</strong> glyph code for <see cref="MediaPlaybackAutoRepeatMode.Track"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="repeatMode"/> is not one of the valid <see cref="MediaPlaybackAutoRepeatMode"/> value.</exception>
    public static string ToRepeatGlyph(MediaPlaybackAutoRepeatMode repeatMode)
    {
        return repeatMode switch
        {
            MediaPlaybackAutoRepeatMode.None => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F0127" : "\uF5E7",
            MediaPlaybackAutoRepeatMode.List => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F004E" : "\uE8EE",
            MediaPlaybackAutoRepeatMode.Track => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F004D" : "\uE8ED",
            _ => throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null),
        };
    }

    /// <summary>
    /// Gets the speed glyph code based on the current player playback rate.
    /// </summary>
    /// <param name="speed">A <see langword="double"/> value that specifies the playback rate.</param>
    /// <returns>
    /// <strong>Speed Medium</strong> glyph if PlaybackSpeed equals 1 x, <strong>Speed High</strong> glyph if its greater than 1 x,
    /// <strong>Auto Racing</strong> glyph if its greater than 1.75 x, <strong>Speed Low</strong> glyph if its less than 1 x, <strong>Speed Off</strong> glyph if its less than 0.25 x.
    /// </returns>
    public static string ToSpeedGlyph(double speed)
    {
        return speed switch
        {
            >= 1.75 => "\uEB24",
            > 1.01 => "\uEC4A",
            <= 0.25 => "\uEC48",
            < 0.99 => "\U000F00A4",
            _ => "\uEC49"
        };
    }

    /// <summary>
    /// Gets the recent glyph code based on a boolean condition.
    /// </summary>
    /// <param name="value">A <see cref="bool"/> that specifies the show recent setting.</param>
    /// <returns>
    /// <strong>Recent</strong> glyph code <see cref="string"/> if the <paramref name="value"/> is <see langword="true"/>;
    /// otherwise, <strong>Recent Empty</strong> glyph code.
    /// </returns>
    public static string ToRecentGlyph(bool value)
    {
        return value ? "\U000F00F0" : "\U000F00F1";
    }

    /// <summary>
    /// Gets the playing state gylph code based on a boolean condition.
    /// </summary>
    /// <param name="value">A <see cref="bool"/> that represents the playing state.</param>
    /// <returns>
    /// <strong>Play</strong> glyph code <see cref="string"/> if the <paramref name="value"/> is <see langword="true"/>;
    /// otherwise, <strong>Pause</strong> glyph code.
    /// </returns>
    public static string ToPlayPauseGlyph(bool value)
    {
        return value ? "\uE769" : "\uE768";
    }

    /// <summary>
    /// Gets the playing state solid gylph code based on a boolean condition.
    /// </summary>
    /// <param name="value">A <see cref="bool"/> that represents the playing state.</param>
    /// <returns>
    /// <strong>Play Solid</strong> glyph code <see cref="string"/> if the <paramref name="value"/> is <see langword="true"/>;
    /// otherwise, <strong>Pause Solid</strong> glyph code.
    /// </returns>
    public static string ToPlayPauseSolidGlyph(bool value)
    {
        return value ? "\uE62E" : "\uF5B0";
    }
}
