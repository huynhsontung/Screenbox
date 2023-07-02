using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using System;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Helpers
{
    internal static class MessengerExtensions
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

        public static void SendPositionStatus(this IMessenger messenger, TimeSpan position, TimeSpan duration, string extra = "")
        {
            string text = string.IsNullOrEmpty(extra)
                ? $"{Humanizer.ToDuration(position)} / {Humanizer.ToDuration(duration)}"
                : $"{Humanizer.ToDuration(position)} / {Humanizer.ToDuration(duration)} ({extra})";
            messenger.Send(new UpdateStatusMessage(text));
        }

        public static void SendSeekWithStatus(this IMessenger messenger, TimeSpan amount)
        {
            PositionChangedResult result =
                messenger.Send(new ChangeTimeRequestMessage(amount, true, false));

            TimeSpan offset = result.NewPosition - result.OriginalPosition;
            string extra = $"{(offset > TimeSpan.Zero ? '+' : string.Empty)}{Humanizer.ToDuration(offset)}";
            messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
        }
    }
}
