using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages;

public class RaiseResumePositionNotificationMessage : ValueChangedMessage<TimeSpan>
{
    public RaiseResumePositionNotificationMessage(TimeSpan value) : base(value)
    {
    }
}
