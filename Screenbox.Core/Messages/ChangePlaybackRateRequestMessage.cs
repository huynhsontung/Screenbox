using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a request message to change the playback rate of the media player.
/// </summary>
public sealed class ChangePlaybackRateRequestMessage : RequestMessage<double>
{
    /// <summary>
    /// Gets the playback rate value or offset to apply.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets a value that indicates whether <see cref="Value"/> is an offset
    /// to the current playback rate.
    /// </summary>
    public bool IsOffset { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePlaybackRateRequestMessage"/> class.
    /// </summary>
    /// <param name="value">The playback rate value or offset to apply.</param>
    /// <param name="isOffset">
    /// <see langword="true"/> to treat <paramref name="value"/> as an offset to the
    /// current playback rate; otherwise, as an absolute playback rate.
    /// </param>
    public ChangePlaybackRateRequestMessage(double value, bool isOffset = false)
    {
        Value = value;
        IsOffset = isOffset;
    }
}
