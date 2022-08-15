#nullable enable

using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.ViewModels;

namespace Screenbox.Core.Messages
{
    internal class PlayMediaMessage : ValueChangedMessage<object>
    {
        public PlayMediaMessage(object value) : base(value)
        {
        }
    }
}
