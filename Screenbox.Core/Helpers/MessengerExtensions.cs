using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Helpers;

public static class MessengerExtensions
{
    private static CancellationTokenSource? s_playFilesCts;
    public static void SendQueueAndPlay(this IMessenger messenger, MediaViewModel media,
        IReadOnlyList<MediaViewModel> queue, bool pauseIfExists = true)
    {
        if (media.IsMediaActive && pauseIfExists)
        {
            messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        var playlist = new Playlist(media, queue);
        messenger.Send(new SetQueueMessage(playlist, true));
    }

    public static void SendPlayNext(this IMessenger messenger, MediaViewModel media)
    {
        Playlist playlist = messenger.Send(new QueueRequestMessage());
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
            messenger.Send(new SetQueueMessage(playlist, false));
        }
    }

    public static void SendPlayNext(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        Playlist playlist = messenger.Send(new QueueRequestMessage());
        if (playlist.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedPlaylist = new Playlist(0, items);
            messenger.Send(new SetQueueMessage(updatedPlaylist, true));
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

            messenger.Send(new SetQueueMessage(playlist, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, MediaViewModel media)
    {
        Playlist playlist = messenger.Send(new QueueRequestMessage());
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
            messenger.Send(new SetQueueMessage(playlist, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        Playlist playlist = messenger.Send(new QueueRequestMessage());
        if (playlist.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedPlaylist = new Playlist(0, items);
            messenger.Send(new SetQueueMessage(updatedPlaylist, true));
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

            messenger.Send(new SetQueueMessage(playlist, false));
        }
    }

    public static void SendPositionStatus(this IMessenger messenger, TimeSpan position, TimeSpan duration, string extra = "", bool showBadge = false)
    {
        bool hasExtra = !string.IsNullOrEmpty(extra);
        var kind = hasExtra && extra.StartsWith("-", StringComparison.Ordinal)
            ? PlaybackCommandKind.Rewind
            : PlaybackCommandKind.FastForward;

        var sb = new StringBuilder(64);
        sb.Append(Humanizer.ToDuration(position)).Append(" / ").Append(Humanizer.ToDuration(duration));

        if (hasExtra)
            sb.Append(" (").Append(extra).Append(')');

        messenger.Send(new PlayerOsdUpdateMessage(kind, value: sb.ToString()) { HasBadge = hasExtra && showBadge, HasMessage = true });
    }

    public static void SendSeekWithStatus(this IMessenger messenger, TimeSpan amount)
    {
        PositionChangedResult result =
            messenger.Send(new ChangeTimeRequestMessage(amount, true, false));

        TimeSpan offset = result.NewPosition - result.OriginalPosition;
        var sb = new StringBuilder(16);

        if (offset.Ticks >= 0)
            sb.Append('+');

        sb.Append(Humanizer.ToDuration(offset));

        messenger.SendPositionStatus(
            result.NewPosition,
            result.NaturalDuration,
            extra: sb.ToString(),
            showBadge: true);
    }

    /// <summary>
    /// Parses storage items into a playlist and sends a message to load and play them.
    /// If a single file was selected and folder auto-enqueue is enabled, automatically
    /// fetches and enqueues neighboring files in the folder.
    /// </summary>
    public static async Task PlayFilesAsync(
        this IMessenger messenger,
        IReadOnlyList<IStorageItem> files,
        IPlaylistService playlistService,
        bool enqueueAllFilesInFolder,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        s_playFilesCts?.Cancel();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        s_playFilesCts = cts;

        try
        {
            var playlist = await playlistService.TryCreatePlaylistAsync(files, neighboringFilesQuery, cts.Token);
            if (playlist is null || playlist.Items.Count == 0 || cts.Token.IsCancellationRequested) return;

            messenger.Send(new SetQueueMessage(playlist, shouldPlay: true));

            if (playlist.Items.Count == 1 && enqueueAllFilesInFolder && playlist.NeighboringFilesQuery is not null)
            {
                try
                {
                    var updatedPlaylist = await playlistService.AddNeighboringFilesAsync(playlist, cts.Token);
                    if (updatedPlaylist.Items.Count > 1 && !cts.Token.IsCancellationRequested)
                    {
                        Playlist currentQueue = messenger.Send(new QueueRequestMessage());
                        if (currentQueue.Items.Count == 1 && currentQueue.CurrentItem == playlist.CurrentItem)
                        {
                            messenger.Send(new SetQueueMessage(updatedPlaylist, shouldPlay: false));
                        }
                    }
                }
                catch (Exception) when (!cts.Token.IsCancellationRequested)
                {
                    // Best-effort background folder expansion; single-file playback is already active.
                }
            }
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer PlayFilesAsync call.
        }
        finally
        {
            if (s_playFilesCts == cts)
            {
                s_playFilesCts = null;
            }
        }
    }
}
