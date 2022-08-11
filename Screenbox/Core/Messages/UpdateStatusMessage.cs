using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public class UpdateStatusMessage : ValueChangedMessage<string>
    {
        public UpdateStatusMessage(string value) : base(value)
        {
        }
    }
}
