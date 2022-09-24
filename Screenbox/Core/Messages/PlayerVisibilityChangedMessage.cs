using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class PlayerVisibilityChangedMessage : ValueChangedMessage<bool>
    {
        public PlayerVisibilityChangedMessage(bool value) : base(value)
        {
        }
    }
}
