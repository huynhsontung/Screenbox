using Windows.Storage;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class RaiseFrameSavedNotificationMessage : ValueChangedMessage<StorageFile>
    {
        public RaiseFrameSavedNotificationMessage(StorageFile value) : base(value)
        {
        }
    }
}
