using System;
using System.Collections.Generic;
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
/// Manages the sliding-window buffer of media items around the current play position,
/// pre-fetching thumbnails and managing the lifecycle of native <see cref="IPlaybackItem"/> instances.
/// </summary>
public sealed partial class PlayQueueBufferManager : IPlayQueueBufferManager
{
    private readonly IPlayerService _playerService;
    private readonly IPlaylistService _playlistService;
    private readonly IMessenger _messenger;
    private readonly ILogger<PlayQueueBufferManager> _logger;

    private readonly Dictionary<MediaViewModel, IPlaybackItem> _bufferedPlaybackItems = new();
    private List<MediaViewModel> _mediaBuffer = new();
    private bool _isDisposed;

    public PlayQueueBufferManager(
        IPlayerService playerService,
        IPlaylistService playlistService,
        ILogger<PlayQueueBufferManager>? logger = null,
        IMessenger? messenger = null)
    {
        _playerService = playerService;
        _playlistService = playlistService;
        _logger = logger ?? NullLogger<PlayQueueBufferManager>.Instance;
        _messenger = messenger ?? WeakReferenceMessenger.Default;
    }

    /// <inheritdoc/>
    public IPlaybackItem? GetOrCreatePlaybackItem(IMediaPlayer? player, MediaViewModel vm)
    {
        if (player is null || _isDisposed) return null;

        if (!_bufferedPlaybackItems.TryGetValue(vm, out var item))
        {
            try
            {
                item = _playerService.CreatePlaybackItem(player, vm.Source, vm.Options.ToArray());
                _bufferedPlaybackItems[vm] = item;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create playback item for {Location}", vm.Location);
                _messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"{vm.Location}{Environment.NewLine}{e.Message}"));
                return null;
            }
        }

        return item;
    }

    /// <inheritdoc/>
    public void CleanPlaybackItem(MediaViewModel vm)
    {
        if (_bufferedPlaybackItems.Remove(vm, out var item))
        {
            item.Dispose();
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
    public void UpdateBuffer(IMediaPlayer? player, Playlist playlist, MediaPlaybackAutoRepeatMode repeatMode)
    {
        if (_isDisposed) return;

        var bufferIndices = _playlistService.GetMediaBufferIndices(playlist.CurrentIndex, playlist.Items.Count, repeatMode);
        var newBuffer = bufferIndices.Select(i => playlist.Items[i]).ToList();

        var toLoad = newBuffer.Except(_mediaBuffer).ToList();
        var toClean = _mediaBuffer.Except(newBuffer).ToList();

        foreach (var media in toClean)
        {
            CleanPlaybackItem(media);
        }

        if (player is not null)
        {
            foreach (var media in newBuffer)
            {
                GetOrCreatePlaybackItem(player, media);
            }
        }

        _mediaBuffer = newBuffer;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Clear();
    }
}
