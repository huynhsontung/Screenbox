#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class UpdateStatusMessage : ValueChangedMessage<string?>
    {
        public UpdateStatusMessage(string? value) : base(value)
        {
        }
    }
}
