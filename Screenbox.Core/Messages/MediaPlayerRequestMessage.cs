#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Playback;

namespace Screenbox.Core.Messages
{
    public sealed class MediaPlayerRequestMessage : RequestMessage<IMediaPlayer?>
    {
    }
}
