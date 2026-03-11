#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when opening media files or a folder for playback fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized error notification.
/// </summary>
public class FailedToOpenFilesNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToOpenFilesNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
