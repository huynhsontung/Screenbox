#nullable enable

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent by a view model when a playlist is successfully renamed.
/// <see cref="ViewModels.NotificationViewModel"/> handles this message and displays
/// a localized success notification.
/// </summary>
public sealed class PlaylistRenamedNotificationMessage
{
    /// <summary>Gets the new display name of the renamed playlist.</summary>
    public string NewName { get; }

    public PlaylistRenamedNotificationMessage(string newName)
    {
        NewName = newName;
    }
}
