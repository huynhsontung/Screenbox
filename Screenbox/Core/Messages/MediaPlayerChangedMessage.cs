using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Messages
{
    internal sealed class MediaPlayerChangedMessage : ValueChangedMessage<IMediaPlayer>
    {
        public MediaPlayerChangedMessage(IMediaPlayer value) : base(value)
        {
        }
    }
}
