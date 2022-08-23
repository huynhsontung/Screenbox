using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class PlayerVisibilityChangedMessage : ValueChangedMessage<bool>
    {
        public PlayerVisibilityChangedMessage(bool value) : base(value)
        {
        }
    }
}
