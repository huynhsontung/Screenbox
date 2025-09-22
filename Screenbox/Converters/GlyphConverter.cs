﻿using System;
using Screenbox.Helpers;
using Windows.Media;

namespace Screenbox.Converters;

/// <summary>
/// Provides <see langword="static"/> methods to convert value types to glyph codes.
/// </summary>
public static partial class GlyphConverter
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
        const string ShuffleGlyph = "\uE8B1";
        const string ShuffleOffGlyph = "\U000F002A";
        const string ShuffleGlyphMirrored = "\U000F0021";
        const string ShuffleOffGlyphMirrored = "\U000F002B";

        return value
            ? (GlobalizationHelper.IsRightToLeftLanguage ? ShuffleGlyphMirrored : ShuffleGlyph)
            : (GlobalizationHelper.IsRightToLeftLanguage ? ShuffleOffGlyphMirrored : ShuffleOffGlyph);
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
        const string RepeatOffGlyph = "\uF5E7";
        const string RepeatOneGlyph = "\uE8ED";
        const string RepeatAllGlyph = "\uE8EE";
        const string RepeatOffMirroredGlyph = "\U000F0127";
        const string RepeatOneMirroredGlyph = "\U000F004D";
        const string RepeatAllMirroredGlyph = "\U000F004E";

        return repeatMode switch
        {
            MediaPlaybackAutoRepeatMode.None => GlobalizationHelper.IsRightToLeftLanguage ? RepeatOffMirroredGlyph : RepeatOffGlyph,
            MediaPlaybackAutoRepeatMode.Track => GlobalizationHelper.IsRightToLeftLanguage ? RepeatOneMirroredGlyph : RepeatOneGlyph,
            MediaPlaybackAutoRepeatMode.List => GlobalizationHelper.IsRightToLeftLanguage ? RepeatAllMirroredGlyph : RepeatAllGlyph,
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
        const string SpeedOffGlyph = "\uEC48";
        const string SpeedLowGlyph = "\U000F00A4";
        const string SpeedMediumGlyph = "\uEC49";
        const string SpeedHighGlyph = "\uEC4A";
        const string AutoRacingGlyph = "\uEB24";

        const double VeryHighSpeed = 1.75;
        const double NormalSpeed = 1.0;
        const double VeryLowSpeed = 0.25;

        const double Tolerance = 0.0001;

        return speed switch
        {
            >= VeryHighSpeed - Tolerance => AutoRacingGlyph,
            > NormalSpeed + Tolerance => SpeedHighGlyph,
            >= NormalSpeed - Tolerance and <= NormalSpeed + Tolerance => SpeedMediumGlyph,
            > VeryLowSpeed + Tolerance => SpeedLowGlyph,
            _ => SpeedOffGlyph,
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
        const string RecentGlyph = "\U000F00F0";
        const string RecentEmptyGlyph = "\U000F00F1";

        return value ? RecentGlyph : RecentEmptyGlyph;
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
        const string PlayGlyph = "\uE768";
        const string PauseGlyph = "\uE769";

        return value ? PauseGlyph : PlayGlyph;
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
        const string PlaySolidGlyph = "\uF5B0";
        const string PauseSolidGlyph = "\uE62E";

        return value ? PauseSolidGlyph : PlaySolidGlyph;
    }

    /// <summary>
    /// Gets the volume glyph code based on mute state and volume value.
    /// </summary>
    /// <param name="isMute">A <see cref="bool"/> that specifies if the player is muted.</param>
    /// <param name="volume">An <see cref="int"/> that specifies the player's volume.</param>
    /// <returns>
    /// <strong>Mute</strong> glyph code <see cref="string"/> if <paramref name="isMute"/> is <see langword="true"/>;
    /// otherwise, a glyph code representing the volume level.
    /// </returns>
    public static string ToVolumeGlyph(bool isMute, int volume)
    {
        const string MuteGlyph = "\uE74F";
        const string Volume0Glyph = "\uE992";
        const string Volume1Glyph = "\uE993";
        const string Volume2Glyph = "\uE994";
        const string Volume3Glyph = "\uE995";

        if (isMute) return MuteGlyph;

        return volume switch
        {
            < 25 => Volume0Glyph,
            < 50 => Volume1Glyph,
            < 75 => Volume2Glyph,
            _ => Volume3Glyph
        };
    }
}
