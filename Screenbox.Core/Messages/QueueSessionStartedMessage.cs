using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Messages;

/// <summary>
/// Broadcast when a new queue origin starts, upgrades in-place from a single file to a folder/sub-item collection,
/// or advances to a new track in an individually-tracked queue.
/// </summary>
public sealed record QueueSessionStartedMessage(
    QueueOrigin Origin,
    MediaViewModel? Media = null,
    string? ReplacedEntryId = null);
