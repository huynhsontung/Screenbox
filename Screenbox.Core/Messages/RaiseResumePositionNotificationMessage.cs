using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    public class RaiseResumePositionNotificationMessage : ValueChangedMessage<TimeSpan>
    {
        public RaiseResumePositionNotificationMessage(TimeSpan value) : base(value)
        {
        }
    }
}
