using Windows.Storage;

namespace Screenbox.Core.Messages;

public sealed class SubtitleAddedNotificationMessage
{
    public SubtitleAddedNotificationMessage(StorageFile file)
    {
        File = file;
    }

    public StorageFile File { get; }
}