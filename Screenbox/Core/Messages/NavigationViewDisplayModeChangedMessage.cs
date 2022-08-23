using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;

namespace Screenbox.Core.Messages
{
    internal class NavigationViewDisplayModeChangedMessage : ValueChangedMessage<NavigationViewDisplayMode>
    {
        public NavigationViewDisplayModeChangedMessage(NavigationViewDisplayMode value) : base(value)
        {
        }
    }
}
