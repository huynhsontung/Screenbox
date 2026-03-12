#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when a playlist is successfully deleted.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized success notification.
/// </summary>
public sealed class PlaylistDeletedNotificationMessage
{
    /// <summary>Gets the display name of the deleted playlist.</summary>
    public string PlaylistName { get; }

    public PlaylistDeletedNotificationMessage(string playlistName)
    {
        PlaylistName = playlistName;
    }
}
