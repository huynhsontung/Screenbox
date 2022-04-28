using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class PlayMediaMessage : ValueChangedMessage<object>
    {
        public PlayMediaMessage(object value) : base(value)
        {
        }
    }
}
