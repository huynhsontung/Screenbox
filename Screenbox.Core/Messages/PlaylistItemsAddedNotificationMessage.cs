namespace Screenbox.Core.Messages;

public sealed class PlaylistItemsAddedNotificationMessage
{
    public string PlaylistName { get; }
    public int ItemCount { get; }

    public PlaylistItemsAddedNotificationMessage(string playlistName, int itemCount)
    {
        PlaylistName = playlistName;
        ItemCount = itemCount;
    }
}
