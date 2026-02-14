using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

/// <summary>
/// Represents a request message to change the playback rate of the media player.
/// </summary>
public sealed class ChangePlaybackRateRequestMessage : RequestMessage<double>
{
    /// <summary>
    /// Gets the playback rate value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePlaybackRateRequestMessage"/> class.
    /// </summary>
    /// <param name="value">The playback rate value.</param>
    public ChangePlaybackRateRequestMessage(double value)
    {
        Value = value;
    }
}
