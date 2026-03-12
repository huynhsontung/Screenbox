#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when media player initialization fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized error notification.
/// </summary>
public class FailedToInitializeNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToInitializeNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
