using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;

using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core
{
    internal static class MessageHelpers
    {
        public static void SendPlayNext(this IMessenger messenger, MediaViewModel media)
        {
            // Clone to prevent queuing duplications
            MediaViewModel clone = media.Clone();
            messenger.Send(new QueuePlaylistMessage(clone, true));
            PlaylistInfo info = messenger.Send(new PlaylistRequestMessage());
            if (info.ActiveIndex == -1)
            {
                messenger.Send(new PlayMediaMessage(clone));
            }
        }
    }
}
