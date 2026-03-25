#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a message that displays a status overlay on the media player.
/// </summary>
public sealed class UpdateStatusMessage : ValueChangedMessage<string?>
{
    /// <summary>
    /// Gets a value that indicates whether the status message remains visible
    /// until explicitly cleared.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the status message persists; otherwise, <see langword="false" />.
    /// The default is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// The status message is cleared by sending an <see cref="UpdateStatusMessage"/>
    /// with a <see langword="null" /> value.
    /// </remarks>
    public bool IsSticky { get; }

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
    /// using the specified text and visibility behavior.
    /// </summary>
    /// <param name="value">The value to set for the status message.</param>
    /// <param name="isSticky"><see langword="true"/> to make the message persistent;
    /// otherwise, <see langword="false"/>.</param>
    public UpdateStatusMessage(string? value, bool isSticky) : this(value)
    {
        IsSticky = isSticky;
    }
}
