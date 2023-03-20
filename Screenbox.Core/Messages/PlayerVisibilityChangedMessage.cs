using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class PlayerVisibilityChangedMessage : ValueChangedMessage<PlayerVisibilityState>
    {
        public PlayerVisibilityChangedMessage(PlayerVisibilityState value) : base(value)
        {
        }
    }
}
