using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class ChangeZoomToFitMessage : ValueChangedMessage<bool>
    {
        public ChangeZoomToFitMessage(bool value) : base(value)
        {
        }
    }
}
