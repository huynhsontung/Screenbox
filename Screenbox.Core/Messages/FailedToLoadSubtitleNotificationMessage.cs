#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when loading a subtitle file fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized error notification.
/// </summary>
public class FailedToLoadSubtitleNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToLoadSubtitleNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
