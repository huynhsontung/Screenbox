#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent to replace the contents of the global play queue with the provided <see cref="Playlist"/> snapshot.
/// </summary>
public sealed class SetQueueMessage : ValueChangedMessage<Playlist>
{
    public bool ShouldPlay { get; }

    public SetQueueMessage(Playlist playlist, bool shouldPlay = false) : base(playlist)
    {
        ShouldPlay = shouldPlay;
    }
}
