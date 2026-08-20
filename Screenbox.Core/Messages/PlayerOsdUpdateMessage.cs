using System;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents an update for the player on-screen display (OSD) message and badge state.
/// </summary>
public sealed class PlayerOsdUpdateMessage
{
    /// <summary>
    /// Gets the playback command associated with the update.
    /// </summary>
    /// <value>A value of the enumeration that specifies the playback action associated with the update.</value>
    public PlaybackCommandKind Kind { get; init; }

    /// <summary>
    /// Gets the payload associated with the update.
    /// </summary>
    /// <value>
    /// The data value to display or process with the update, or <see langword="null"/>
    /// if no value is set.
    /// </value>
    public object? Value { get; init; }

    /// <summary>
    /// Gets the display duration for the update.
    /// </summary>
    /// <value>
    /// The duration of the update. The default is <c>1</c> second.
    /// </value>
    public TimeSpan Duration { get; private set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets a value indicating whether the update includes a message.
    /// </summary>
    /// <value><see langword="true"/> to display a message; otherwise, <see langword="false"/>.</value>
    public bool HasMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the update includes a badge.
    /// </summary>
    /// <value><see langword="true"/> to display the badge; otherwise, <see langword="false"/>.</value>
    public bool HasBadge { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerOsdUpdateMessage"/> class
    /// with the specified playback command kind, and value.
    /// </summary>
    /// <param name="kind">A value of the enumeration that specifies the playback command associated with the update.</param>
    /// <param name="value">The payload to associate with the update, or <see langword="null"/> if no value is provided.</param>
    public PlayerOsdUpdateMessage(PlaybackCommandKind kind, object? value = null)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>
    /// Show the OSD badge.
    /// </summary>
    /// <returns>
    /// Returns the <see cref="PlayerOsdUpdateMessage"/> instance so that additional method calls can be chained.
    /// </returns>
    public PlayerOsdUpdateMessage ShowBadge()
    {
        HasBadge = true;
        return this;
    }

    /// <summary>
    /// Show the OSD message.
    /// </summary>
    /// <returns>
    /// Returns the <see cref="PlayerOsdUpdateMessage"/> instance so that additional method calls can be chained.
    /// </returns>
    public PlayerOsdUpdateMessage ShowMessage()
    {
        HasMessage = true;
        return this;
    }

    /// <summary>
    /// Sets the display duration for the OSD update.
    /// </summary>
    /// <param name="duration">The display duration to set.</param>
    /// <returns>
    /// Returns the <see cref="PlayerOsdUpdateMessage"/> instance so that additional method calls can be chained.
    /// </returns>
    public PlayerOsdUpdateMessage SetDuration(TimeSpan duration)
    {
        Duration = duration;
        return this;
    }
}
