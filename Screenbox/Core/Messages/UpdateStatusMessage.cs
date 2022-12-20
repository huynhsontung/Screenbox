using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class UpdateStatusMessage : ValueChangedMessage<string?>
    {
        public bool Persistent { get; }

        public UpdateStatusMessage(string? value, bool persistent = false) : base(value)
        {
            Persistent = persistent;
        }
    }
}
