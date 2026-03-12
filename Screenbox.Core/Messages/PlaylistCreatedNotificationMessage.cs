#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when a new playlist is successfully created.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized success notification.
/// </summary>
public sealed class PlaylistCreatedNotificationMessage
{
    /// <summary>Gets the display name of the newly created playlist.</summary>
    public string PlaylistName { get; }

    public PlaylistCreatedNotificationMessage(string playlistName)
    {
        PlaylistName = playlistName;
    }
}
