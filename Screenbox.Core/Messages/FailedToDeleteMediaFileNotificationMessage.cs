#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when deleting a media file from disk fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message
/// </summary>
public sealed class FailedToDeleteMediaFileNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToDeleteMediaFileNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
