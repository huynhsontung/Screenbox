using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Controls;

namespace Screenbox.Core.Messages
{
    internal sealed class PlayerVisibilityChangedMessage : ValueChangedMessage<PlayerVisibilityStates>
    {
        public PlayerVisibilityChangedMessage(PlayerVisibilityStates value) : base(value)
        {
        }
    }
}
