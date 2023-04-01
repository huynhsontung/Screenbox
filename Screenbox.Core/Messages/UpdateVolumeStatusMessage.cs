using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class UpdateVolumeStatusMessage : ValueChangedMessage<int>
    {
        public bool Persistent { get; }

        public UpdateVolumeStatusMessage(int value, bool persistent) : base(value)
        {
            Persistent = persistent;
        }
    }
}
