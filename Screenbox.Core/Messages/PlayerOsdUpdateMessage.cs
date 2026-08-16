using System;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

public sealed record PlayerOsdUpdateMessage(
    PlaybackCommandKind Kind,
    object? Value = null,
    TimeSpan? Duration = null,
    bool HasMessage = false,
    bool HasBadge = false)
{
    public PlayerOsdUpdateMessage WithBadge() =>
        this with { HasBadge = true };

    //public PlayerOsdUpdateMessage WithDuration(TimeSpan duration) =>
    //    this with { Duration = duration };

    public PlayerOsdUpdateMessage WithMessage() =>
        this with { HasMessage = true };

    //public PlayerOsdUpdateMessage WithValue(object value) =>
    //    this with { Value = value };
}
