using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Models;
using System;

namespace Screenbox.Core.Messages
{
    public sealed class ChangeTimeRequestMessage : RequestMessage<PositionChangedResult>
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
