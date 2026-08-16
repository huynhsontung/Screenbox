using System;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents an update for the player on-screen display (OSD) message and badge state.
/// </summary>
/// <param name="Kind">A value of the enumeration that specifies the playback action associated with the update.</param>
/// <param name="Value">The display value associated with the update, such as a volume level or time string.</param>
/// <param name="Duration">The custom display duration for the update, if one is specified.</param>
/// <param name="HasMessage"><see langword="true"/> to display the message text; otherwise, <see langword="false"/>.</param>
/// <param name="HasBadge"><see langword="true"/> to show the command badge; otherwise, <see langword="false"/>.</param>
public sealed record PlayerOsdUpdateMessage(
    PlaybackCommandKind Kind,
    object? Value = null,
    TimeSpan? Duration = null,
    bool HasMessage = false,
    bool HasBadge = false)
{
    /// <summary>
    /// Creates a <see cref="PlayerOsdUpdateMessage"/> instance with the <see cref="HasBadge"/>
    /// property set to <see langword="true"/>.
    /// </summary>
    /// <returns>A new <see cref="PlayerOsdUpdateMessage"/> instance.</returns>
    public PlayerOsdUpdateMessage WithBadge() =>
        this with { HasBadge = true };

    //public PlayerOsdUpdateMessage WithDuration(TimeSpan duration) =>
    //    this with { Duration = duration };

    /// <summary>
    /// Creates a <see cref="PlayerOsdUpdateMessage"/> instance with the <see cref="HasMessage"/>
    /// property set to <see langword="true"/>.
    /// </summary>
    /// <returns>A new <see cref="PlayerOsdUpdateMessage"/> instance.</returns>
    public PlayerOsdUpdateMessage WithMessage() =>
        this with { HasMessage = true };

    //public PlayerOsdUpdateMessage WithValue(object value) =>
    //    this with { Value = value };
}
