using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Helpers;

public static class MessengerExtensions
{
    private static CancellationTokenSource? s_playFilesCts;
    public static void SendQueueAndPlay(
        this IMessenger messenger,
        MediaViewModel media,
        IReadOnlyList<MediaViewModel> queue,
        bool pauseIfExists = true,
        QueueOrigin? origin = null)
    {
        if (media.IsMediaActive && pauseIfExists)
        {
            messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        var session = new QueueSession(media, queue)
        {
            Origin = origin
        };
        messenger.Send(new SetQueueMessage(session, true));
    }

    public static void SendShuffleAndPlay(
        this IMessenger messenger,
        IReadOnlyList<MediaViewModel> items,
        QueueOrigin? origin = null)
    {
        if (items.Count == 0) return;

        var originalItems = items.ToList();
        var shuffledItems = new List<MediaViewModel>(originalItems);
        var random = new Random();
        for (int n = shuffledItems.Count - 1; n > 0; n--)
        {
            int k = random.Next(n + 1);
            (shuffledItems[k], shuffledItems[n]) = (shuffledItems[n], shuffledItems[k]);
        }

        var session = new QueueSession(0, shuffledItems)
        {
            ShuffleMode = true,
            ShuffleBackup = new ShuffleBackup(originalItems),
            Origin = origin ?? QueueOrigin.ForLibraryQueue(shuffledItems[0])
        };
        messenger.Send(new SetQueueMessage(session, true));
    }

    public static void SendPlayNext(this IMessenger messenger, MediaViewModel media)
    {
        QueueSession queue = messenger.Send(new QueueRequestMessage());
        if (queue.IsEmpty)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(media));
        }
        else
        {
            // Clone to prevent queuing duplications
            MediaViewModel clone = new(media);
            queue.Items.Insert(Math.Min(queue.CurrentIndex + 1, queue.Items.Count), clone);
            messenger.Send(new SetQueueMessage(queue, false));
        }
    }

    public static void SendPlayNext(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        QueueSession queue = messenger.Send(new QueueRequestMessage());
        if (queue.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedQueue = new QueueSession(0, items)
            {
                Origin = items.Count > 1
                    ? QueueOrigin.ForMediaCollection(items[0].Name)
                    : QueueOrigin.ForSingleMedia(items[0])
            };
            messenger.Send(new SetQueueMessage(updatedQueue, true));
        }
        else
        {
            // Clone all items to prevent queuing duplications
            List<MediaViewModel> clones = items.Select(item => new MediaViewModel(item)).ToList();

            int insertIndex = Math.Min(queue.CurrentIndex + 1, queue.Items.Count);

            // Insert items in order at the insertion point
            for (int i = 0; i < clones.Count; i++)
            {
                queue.Items.Insert(insertIndex + i, clones[i]);
            }

            messenger.Send(new SetQueueMessage(queue, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, MediaViewModel media)
    {
        QueueSession queue = messenger.Send(new QueueRequestMessage());
        if (queue.IsEmpty)
        {
            // Play the item on its own
            messenger.Send(new PlayMediaMessage(media));
        }
        else
        {
            // Clone to prevent queuing duplications
            MediaViewModel clone = new(media);
            queue.Items.Add(clone);
            messenger.Send(new SetQueueMessage(queue, false));
        }
    }

    public static void SendAddToQueue(this IMessenger messenger, IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;

        QueueSession queue = messenger.Send(new QueueRequestMessage());
        if (queue.IsEmpty)
        {
            // Queue all items and play the first one
            var updatedQueue = new QueueSession(0, items)
            {
                Origin = items.Count > 1
                    ? QueueOrigin.ForMediaCollection(items[0].Name)
                    : QueueOrigin.ForSingleMedia(items[0])
            };
            messenger.Send(new SetQueueMessage(updatedQueue, true));
        }
        else
        {
            // Clone all items to prevent queuing duplications
            var clones = items.Select(item => new MediaViewModel(item)).ToList();

            // Add all items to the end of the queue
            foreach (MediaViewModel clone in clones)
            {
                queue.Items.Add(clone);
            }

            messenger.Send(new SetQueueMessage(queue, false));
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
    /// Parses storage items into a queue session and sends a message to load and play them.
    /// If a single file was selected and folder auto-enqueue is enabled, automatically
    /// fetches and enqueues neighboring files in the folder.
    /// </summary>
    public static async Task PlayFilesAsync(
        this IMessenger messenger,
        IReadOnlyList<IStorageItem> files,
        IQueueService queueService,
        bool enqueueAllFilesInFolder,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        s_playFilesCts?.Cancel();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        s_playFilesCts = cts;

        try
        {
            var queue = await queueService.TryCreateQueueAsync(files, neighboringFilesQuery, cts.Token);
            if (queue is null || queue.Items.Count == 0 || cts.Token.IsCancellationRequested) return;

            messenger.Send(new SetQueueMessage(queue, shouldPlay: true));

            if (queue.Items.Count == 1 && enqueueAllFilesInFolder && queue.NeighboringFilesQuery is not null)
            {
                try
                {
                    var updatedQueue = await queueService.AddNeighboringFilesAsync(queue, cts.Token);
                    if (updatedQueue.Items.Count > 1 && !cts.Token.IsCancellationRequested)
                    {
                        QueueSession currentQueue = messenger.Send(new QueueRequestMessage());
                        if (currentQueue.Items.Count == 1 && currentQueue.CurrentItem == queue.CurrentItem)
                        {
                            messenger.Send(new SetQueueMessage(updatedQueue, shouldPlay: false));
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
