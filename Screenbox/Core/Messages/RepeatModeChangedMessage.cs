using Windows.Media;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class RepeatModeChangedMessage : ValueChangedMessage<MediaPlaybackAutoRepeatMode>
    {
        public RepeatModeChangedMessage(MediaPlaybackAutoRepeatMode value) : base(value)
        {
        }
    }
}
