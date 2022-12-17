using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class PlayMediaMessage : ValueChangedMessage<object>
    {
        public bool Existing { get; }

        public PlayMediaMessage(object value, bool existing = false) : base(value)
        {
            Existing = existing;
        }
    }
}
