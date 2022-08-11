using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class ChangeZoomToFitMessage : ValueChangedMessage<bool>
    {
        public ChangeZoomToFitMessage(bool value) : base(value)
        {
        }
    }
}
