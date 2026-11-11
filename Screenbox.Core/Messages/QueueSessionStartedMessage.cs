using Screenbox.Core.Models;

namespace Screenbox.Core.Messages;

/// <summary>
/// Broadcast when a new queue session is started, upgraded in-place from a single file to a folder/sub-item collection,
/// or advanced to a new track in an individually-tracked queue.
/// </summary>
public sealed record QueueSessionStartedMessage(QueueSession Queue, bool IsUpgrade = false);
