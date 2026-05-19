using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.Messages;

/// <summary>
/// Requests a snapshot of the current play queue as a <see cref="Playlist"/> model.
/// </summary>
public sealed class QueueRequestMessage : RequestMessage<Playlist>
{
}
