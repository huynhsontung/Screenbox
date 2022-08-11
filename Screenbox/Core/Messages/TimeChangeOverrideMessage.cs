using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class TimeChangeOverrideMessage : ValueChangedMessage<bool>
    {
        public TimeChangeOverrideMessage(bool value) : base(value)
        {
        }
    }
}
