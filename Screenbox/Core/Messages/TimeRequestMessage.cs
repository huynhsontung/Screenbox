using System;

namespace Screenbox.Core.Messages
{
    internal class TimeRequestMessage : ChangeValueRequestMessage<TimeSpan>
    {
        public TimeRequestMessage()
        {
        }

        public TimeRequestMessage(TimeSpan value) : base(value)
        {
        }
    }
}
