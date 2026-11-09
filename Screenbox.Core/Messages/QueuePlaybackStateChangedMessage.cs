using System;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Messages;

/// <summary>
/// Immutable snapshot representing the active play queue session and playback state.
/// </summary>
/// <param name="SessionId">The unique identifier of the active queue session, or <see cref="Guid.Empty"/> if empty.</param>
/// <param name="Origin">The origin metadata of the active queue session.</param>
/// <param name="CurrentItem">The current media item in the play queue, or <see langword="null"/>.</param>
/// <param name="IsPlaying">Whether the current media item in the play queue is currently playing.</param>
public sealed record QueuePlaybackState(
    Guid SessionId,
    QueueOrigin? Origin,
    MediaViewModel? CurrentItem,
    bool IsPlaying);

/// <summary>
/// Broadcast when the play queue session, origin, current media item, or playback status changes.
/// </summary>
public sealed record QueuePlaybackStateChangedMessage(QueuePlaybackState State);

/// <summary>
/// Request message to query the current play queue playback state on demand.
/// </summary>
public sealed class QueuePlaybackStateRequestMessage : RequestMessage<QueuePlaybackState>;
