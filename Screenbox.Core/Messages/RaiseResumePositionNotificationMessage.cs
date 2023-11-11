using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Screenbox.Core.Messages
{
    public class RaiseResumePositionNotificationMessage : ValueChangedMessage<TimeSpan>
    {
        public RaiseResumePositionNotificationMessage(TimeSpan value) : base(value)
        {
        }
    }
}
