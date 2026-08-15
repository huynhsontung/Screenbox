using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.Media;

namespace Screenbox.Core.Messages;

public sealed class RepeatModeChangedMessage : ValueChangedMessage<MediaPlaybackAutoRepeatMode>
{
    public RepeatModeChangedMessage(MediaPlaybackAutoRepeatMode value) : base(value)
    {
    }
}
