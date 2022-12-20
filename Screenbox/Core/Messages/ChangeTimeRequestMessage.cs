using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class ChangeTimeRequestMessage : RequestMessage<TimeSpan>
    {
        public bool IsOffset { get; }

        public TimeSpan Value { get; }

        public ChangeTimeRequestMessage(TimeSpan value, bool isOffset = false)
        {
            Value = value;
            IsOffset = isOffset;
        }
    }
}
