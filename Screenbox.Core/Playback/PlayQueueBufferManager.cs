using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Playback;

/// <summary>
/// Manages the sliding-window buffer of media items around the current play position
/// and the lifecycle of native <see cref="IPlaybackItem"/> instances.
/// </summary>
public sealed partial class PlayQueueBufferManager : IPlayQueueBufferManager
{
    /// <inheritdoc/>
    public IReadOnlyList<QueueItemViewModel> BufferedItems => _mediaBuffer;

    private readonly IPlayerService _playerService;
    private readonly IQueueService _queueService;
    private readonly IMessenger _messenger;
    private readonly ILogger<PlayQueueBufferManager> _logger;

    private readonly Dictionary<QueueItemViewModel, IPlaybackItem> _bufferedPlaybackItems = new();
    private List<QueueItemViewModel> _mediaBuffer = new();
    private bool _isDisposed;

    public PlayQueueBufferManager(
        IPlayerService playerService,
        IQueueService queueService,
        ILogger<PlayQueueBufferManager>? logger = null,
        IMessenger? messenger = null)
    {
        _playerService = playerService;
        _queueService = queueService;
        _logger = logger ?? NullLogger<PlayQueueBufferManager>.Instance;
        _messenger = messenger ?? WeakReferenceMessenger.Default;
    }

    /// <inheritdoc/>
    public bool TryGetPlaybackItem(QueueItemViewModel queueItem, [NotNullWhen(true)] out IPlaybackItem? item)
    {
        if (!_isDisposed && _bufferedPlaybackItems.TryGetValue(queueItem, out item))
        {
            return true;
        }

        item = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetPlaybackItem(MediaViewModel vm, [NotNullWhen(true)] out IPlaybackItem? item)
    {
        if (!_isDisposed)
        {
            foreach (var entry in _bufferedPlaybackItems)
            {
                if (ReferenceEquals(entry.Key.Media, vm))
                {
                    item = entry.Value;
                    return true;
                }
            }
        }

        item = null;
        return false;
    }

    /// <inheritdoc/>
    public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, QueueItemViewModel queueItem)
    {
        if (player is null || _isDisposed)
        {
            return null;
        }

        if (!_bufferedPlaybackItems.TryGetValue(queueItem, out var item))
        {
            MediaViewModel media = queueItem.Media;
            try
            {
                item = _playerService.CreatePlaybackItem(player, media.Source, media.Options.ToArray());
                _bufferedPlaybackItems[queueItem] = item;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create playback item for {Location}", media.Location);
                _messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"{media.Location}{Environment.NewLine}{e.Message}"));
                return null;
            }
        }

        return item;
    }

    /// <inheritdoc/>
    public void CleanPlaybackItem(QueueItemViewModel queueItem)
    {
        if (_bufferedPlaybackItems.Remove(queueItem, out var item))
        {
            item.Dispose();
        }
    }

    /// <inheritdoc/>
    public void CleanPlaybackItems(MediaViewModel vm, QueueItemViewModel? exclude = null)
    {
        var keysToRemove = _bufferedPlaybackItems.Keys
            .Where(key => ReferenceEquals(key.Media, vm) && !ReferenceEquals(key, exclude))
            .ToList();

        foreach (var key in keysToRemove)
        {
            CleanPlaybackItem(key);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        foreach (var item in _bufferedPlaybackItems.Values)
        {
            item.Dispose();
        }

        _bufferedPlaybackItems.Clear();
        _mediaBuffer.Clear();
    }

    /// <inheritdoc/>
    public void UpdateBuffer(IMediaPlayer? player, QueueSession session, MediaPlaybackAutoRepeatMode repeatMode)
    {
        if (_isDisposed)
        {
            return;
        }

        var bufferIndices = _queueService.GetMediaBufferIndices(session.CurrentIndex, session.Items.Count, repeatMode);
        var newBuffer = bufferIndices.Select(i => session.Items[i]).ToList();

        var toClean = _mediaBuffer.Except(newBuffer).ToList();

        foreach (var item in toClean)
        {
            CleanPlaybackItem(item);
        }

        if (player is not null)
        {
            foreach (var item in newBuffer)
            {
                GetOrCreatePlaybackItem(player, item);
            }
        }

        _mediaBuffer = newBuffer;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Clear();
    }
}
