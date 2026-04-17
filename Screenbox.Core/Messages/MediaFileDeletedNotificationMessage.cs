#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when a media file is successfully deleted from disk.
/// <see cref="ViewModels.NotificationViewModel"/> displays a localized success notification.
/// </summary>
public sealed class MediaFileDeletedNotificationMessage
{
    /// <summary>Gets the display name of the deleted media file.</summary>
    public string FileName { get; }

    public MediaFileDeletedNotificationMessage(string fileName)
    {
        FileName = fileName;
    }
}
