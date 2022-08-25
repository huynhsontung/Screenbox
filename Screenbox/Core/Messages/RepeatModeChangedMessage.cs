using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.ViewModels;

namespace Screenbox.Core.Messages
{
    internal class RepeatModeChangedMessage : ValueChangedMessage<RepeatMode>
    {
        public RepeatModeChangedMessage(RepeatMode value) : base(value)
        {
        }
    }
}
