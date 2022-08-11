#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public class ChangeValueRequestMessage<T> : RequestMessage<T>
    {
        public bool IsChangeRequest { get; }

        public T? Value { get; }

        public ChangeValueRequestMessage()
        {
        }

        public ChangeValueRequestMessage(T value)
        {
            Value = value;
            IsChangeRequest = true;
        }
    }
}
