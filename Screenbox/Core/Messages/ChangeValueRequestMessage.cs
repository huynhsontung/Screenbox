#nullable enable

using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class ChangeValueRequestMessage<T> : RequestMessage<T>
    {
        public bool IsChangeRequest { get; set; }

        public T? Value { get; set; }
    }
}
