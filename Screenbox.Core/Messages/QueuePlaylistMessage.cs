#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.Messages;

public sealed class QueuePlaylistMessage : ValueChangedMessage<Playlist>
{
    public bool ShouldPlay { get; }

    public QueuePlaylistMessage(Playlist playlist, bool shouldPlay = false) : base(playlist)
    {
        ShouldPlay = shouldPlay;
    }
}
