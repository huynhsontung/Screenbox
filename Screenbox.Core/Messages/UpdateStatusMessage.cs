#nullable enable

using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a message that displays a status overlay on the media player.
/// </summary>
public sealed class UpdateStatusMessage : ValueChangedMessage<string?>
{
    /// <summary>
    /// Gets the duration of the status message.
    /// </summary>
    /// <value>The duration of the message. The default is 1 second.</value>
    public TimeSpan Duration { get; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatusMessage"/> class
    /// using the specified text.
    /// </summary>
    /// <param name="value">The value to set for the status message.</param>
    public UpdateStatusMessage(string? value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatusMessage"/> class
    /// using the specified text and display time.
    /// </summary>
    /// <param name="value">The value to set for the status message.</param>
    /// <param name="duration">The duration of the status message.</param>
    public UpdateStatusMessage(string? value, TimeSpan duration) : this(value)
    {
        Duration = duration;
    }
}
