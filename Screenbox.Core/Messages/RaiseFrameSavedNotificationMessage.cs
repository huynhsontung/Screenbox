using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.Storage;

namespace Screenbox.Core.Messages;

public sealed class RaiseFrameSavedNotificationMessage : ValueChangedMessage<StorageFile>
{
    public RaiseFrameSavedNotificationMessage(StorageFile value) : base(value)
    {
    }
}
