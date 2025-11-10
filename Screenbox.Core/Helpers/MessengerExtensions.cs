#nullable enable

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Helpers;

internal static class MessengerExtensions
{
    public static void SendQueueAndPlay(this IMessenger messenger, MediaViewModel media,
        IReadOnlyList<MediaViewModel> queue, bool pauseIfExists = true)
    {
        Playlist playlist = messenger.Send(new PlaylistRequestMessage());
        if (media.IsMediaActive && pauseIfExists)
        {
            messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        var updatedPlaylist = new Playlist(media, queue, playlist);
        messenger.Send(new QueuePlaylistMessage(updatedPlaylist, true));
    }

    public static void SendPlayNext(this IMessenger messenger, MediaViewModel media)
    {
        // Clone to prevent queuing duplications
        MediaViewModel clone = new(media);
        Playlist playlist = messenger.Send(new PlaylistRequestMessage());

        // If current index < 0 then the current playlist is empty
        if (playlist.CurrentIndex < 0)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(clone));
        }
        else
        {
            var updatedPlaylist = new Playlist(playlist);
            updatedPlaylist.Items.Insert(Math.Min(playlist.CurrentIndex + 1, playlist.Items.Count), clone);
            messenger.Send(new QueuePlaylistMessage(updatedPlaylist, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, MediaViewModel media)
    {
        // Clone to prevent queuing duplications
        MediaViewModel clone = new(media);
        Playlist playlist = messenger.Send(new PlaylistRequestMessage());

        // If current index < 0 then the current playlist is empty
        if (playlist.CurrentIndex < 0)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(clone));
        }
        else
        {
            var updatedPlaylist = new Playlist(playlist);
            updatedPlaylist.Items.Add(clone);
            messenger.Send(new QueuePlaylistMessage(updatedPlaylist, false));
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
