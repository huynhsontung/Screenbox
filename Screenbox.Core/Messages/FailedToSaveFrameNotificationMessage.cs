#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when saving a video frame snapshot fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized error notification.
/// </summary>
public class FailedToSaveFrameNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToSaveFrameNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
