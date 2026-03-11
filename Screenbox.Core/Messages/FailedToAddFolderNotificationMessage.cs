#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when adding a folder to a media library fails.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized error notification.
/// </summary>
public class FailedToAddFolderNotificationMessage
{
    /// <summary>Gets the error detail from the underlying exception, or <c>null</c> if unavailable.</summary>
    public string? Reason { get; }

    public FailedToAddFolderNotificationMessage(string? reason = null)
    {
        Reason = reason;
    }
}
