using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class ChangeZoomToFitMessage : ValueChangedMessage<bool>
    {
        public ChangeZoomToFitMessage(bool value) : base(value)
        {
        }
    }
}
