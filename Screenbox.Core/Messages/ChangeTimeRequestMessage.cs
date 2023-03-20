using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public sealed class ChangeTimeRequestMessage : RequestMessage<TimeSpan>
    {
        public bool Debounce { get; }

        public bool IsOffset { get; }

        public TimeSpan Value { get; }

        public ChangeTimeRequestMessage(TimeSpan value, bool isOffset = false, bool debounce = true)
        {
            Value = value;
            IsOffset = isOffset;
            Debounce = debounce;
        }
    }
}
