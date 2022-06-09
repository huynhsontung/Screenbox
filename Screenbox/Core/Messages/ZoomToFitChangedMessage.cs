using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class ZoomToFitChangedMessage : ValueChangedMessage<bool>
    {
        public ZoomToFitChangedMessage(bool value) : base(value)
        {
        }
    }
}
