using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class UpdateVolumeStatusMessage : ValueChangedMessage<int>
    {
        public UpdateVolumeStatusMessage(int value) : base(value)
        {
        }
    }
}
