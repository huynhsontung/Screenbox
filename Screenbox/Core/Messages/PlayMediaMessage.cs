#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class PlayMediaMessage : ValueChangedMessage<object>
    {
        public PlayMediaMessage(object value) : base(value)
        {
        }
    }
}
