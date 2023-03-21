using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Messages
{
    public sealed class PlayerVisibilityChangedMessage : ValueChangedMessage<PlayerVisibilityState>
    {
        public PlayerVisibilityChangedMessage(PlayerVisibilityState value) : base(value)
        {
        }
    }
}
