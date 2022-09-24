using System;

namespace Screenbox.Core.Messages
{
    internal sealed class TimeRequestMessage : ChangeValueRequestMessage<TimeSpan>
    {
        public TimeRequestMessage()
        {
        }

        public TimeRequestMessage(TimeSpan value) : base(value)
        {
        }
    }
}
