#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
        if (media.IsMediaActive && pauseIfExists)
        {
            messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        var playlist = new Playlist(media, queue);
        messenger.Send(new QueuePlaylistMessage(playlist, true));
    }

    public static void SendPlayNext(this IMessenger messenger, MediaViewModel media)
    {
        Playlist playlist = messenger.Send(new PlaylistRequestMessage());
        if (playlist.IsEmpty)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(media));
        }
        else
        {
            // Clone to prevent queuing duplications
            MediaViewModel clone = new(media);
            playlist.Items.Insert(Math.Min(playlist.CurrentIndex + 1, playlist.Items.Count), clone);
            messenger.Send(new QueuePlaylistMessage(playlist, false));
        }
    }

    public static void SendPlayNext(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        Playlist playlist = messenger.Send(new PlaylistRequestMessage());
        if (playlist.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedPlaylist = new Playlist(0, items);
            messenger.Send(new QueuePlaylistMessage(updatedPlaylist, true));
        }
        else
        {
            // Clone all items to prevent queuing duplications
            List<MediaViewModel> clones = items.Select(item => new MediaViewModel(item)).ToList();

            int insertIndex = Math.Min(playlist.CurrentIndex + 1, playlist.Items.Count);

            // Insert items in order at the insertion point
            for (int i = 0; i < clones.Count; i++)
            {
                playlist.Items.Insert(insertIndex + i, clones[i]);
            }

            messenger.Send(new QueuePlaylistMessage(playlist, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, MediaViewModel media)
    {
        Playlist playlist = messenger.Send(new PlaylistRequestMessage());
        if (playlist.IsEmpty)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(media));
        }
        else
        {
            // Clone to prevent queuing duplications
            MediaViewModel clone = new(media);
            playlist.Items.Add(clone);
            messenger.Send(new QueuePlaylistMessage(playlist, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        Playlist playlist = messenger.Send(new PlaylistRequestMessage());
        if (playlist.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedPlaylist = new Playlist(0, items);
            messenger.Send(new QueuePlaylistMessage(updatedPlaylist, true));
        }
        else
        {
            // Clone all items to prevent queuing duplications
            var clones = items.Select(item => new MediaViewModel(item)).ToList();

            // Add all items to the end of the queue
            foreach (MediaViewModel clone in clones)
            {
                playlist.Items.Add(clone);
            }

            messenger.Send(new QueuePlaylistMessage(playlist, false));
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
