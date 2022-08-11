using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Messages
{
    internal class MediaPlayerChangedMessage : ValueChangedMessage<IMediaPlayer>
    {
        public MediaPlayerChangedMessage(IMediaPlayer value) : base(value)
        {
        }
    }
}
