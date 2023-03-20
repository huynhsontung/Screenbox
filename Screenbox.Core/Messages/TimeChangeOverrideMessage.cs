using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class TimeChangeOverrideMessage : ValueChangedMessage<bool>
    {
        public TimeChangeOverrideMessage(bool value) : base(value)
        {
        }
    }
}
