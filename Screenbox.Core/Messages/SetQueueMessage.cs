using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent to replace the contents of the global play queue with the provided <see cref="QueueSession"/> snapshot.
/// </summary>
public sealed class SetQueueMessage : ValueChangedMessage<QueueSession>
{
    public bool ShouldPlay { get; }

    public SetQueueMessage(QueueSession queue, bool shouldPlay = false) : base(queue)
    {
        ShouldPlay = shouldPlay;
    }
}
