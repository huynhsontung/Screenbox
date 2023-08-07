#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class SeekBarViewModel :
        ObservableRecipient,
        IRecipient<TimeChangeOverrideMessage>,
        IRecipient<ChangeTimeRequestMessage>,
        IRecipient<PlayerControlsVisibilityChangedMessage>,
        IRecipient<PlayerVisibilityChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private double _length;

        [ObservableProperty] private double _time;

        [ObservableProperty] private bool _isSeekable;

        [ObservableProperty] private bool _bufferingVisible;

        [ObservableProperty] private IReadOnlyCollection<ChapterCue>? _chapters;

        [ObservableProperty] private double _previewTime;

        [ObservableProperty] private bool _shouldShowPreview;

        [ObservableProperty] private bool _shouldHandleKeyDown;

        private IMediaPlayer? _mediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _seekTimer;
        private readonly DispatcherQueueTimer _originalPositionTimer;
        private TimeSpan _originalPosition;
        private bool _timeChangeOverride;
        private bool _debounceOverride;

        public SeekBarViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            _seekTimer = _dispatcherQueue.CreateTimer();
            _originalPositionTimer = _dispatcherQueue.CreateTimer();
            _originalPositionTimer.IsRepeating = false;
            _shouldShowPreview = true;
            _shouldHandleKeyDown = true;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PlayerVisibilityChangedMessage message)
        {
            ShouldHandleKeyDown = message.Value != PlayerVisibilityState.Visible;
        }

        public void Receive(PlayerControlsVisibilityChangedMessage message)
        {
            if (!message.Value && ShouldShowPreview)
            {
                ShouldShowPreview = false;
            }
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.NaturalDurationChanged += OnNaturalDurationChanged;
            _mediaPlayer.PositionChanged += OnPositionChanged;
            _mediaPlayer.MediaEnded += OnEndReached;
            _mediaPlayer.BufferingStarted += OnBufferingStarted;
            _mediaPlayer.BufferingEnded += OnBufferingEnded;
            _mediaPlayer.SourceChanged += OnSourceChanged;
        }

        public void Receive(TimeChangeOverrideMessage message)
        {
            _timeChangeOverride = message.Value;
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            if (!message.Debounce)
                _debounceOverride = true;

            TimeSpan currentPosition = TimeSpan.FromMilliseconds(Time);
            _originalPositionTimer.Debounce(() => _originalPosition = currentPosition, TimeSpan.FromSeconds(1), true);

            // Assume UI thread
            Time = message.IsOffset
                ? Math.Clamp(Time + message.Value.TotalMilliseconds, 0, Length)
                : message.Value.TotalMilliseconds;

            message.Reply(new PositionChangedResult(currentPosition, TimeSpan.FromMilliseconds(Time),
                _originalPosition, TimeSpan.FromMilliseconds(Length)));
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            _timeChangeOverride = pressed;
        }

        public void UpdatePreviewTime(double normalizedPosition)
        {
            normalizedPosition = Math.Clamp(normalizedPosition, 0, 1);
            PreviewTime = (long)(normalizedPosition * Length);
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (IsSeekable && _mediaPlayer != null)
            {
                double currentMs = _mediaPlayer.Position.TotalMilliseconds;
                double newDiffMs = Math.Abs(args.NewValue - currentMs);
                double oldDiffMs = Math.Abs(args.OldValue - currentMs);
                bool shouldUpdate = oldDiffMs < 50 && newDiffMs > 400;
                bool shouldOverride = _timeChangeOverride && newDiffMs > 100;
                bool paused = _mediaPlayer.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Buffering;
                if (shouldUpdate || paused || shouldOverride)
                {
                    SetPlayerPosition(TimeSpan.FromMilliseconds(args.NewValue), !_debounceOverride);
                    _debounceOverride = false;
                }
            }
        }

        private void SetPlayerPosition(TimeSpan position, bool debounce)
        {
            if (!IsSeekable || _mediaPlayer == null) return;
            if (debounce)
            {
                _seekTimer.Debounce(() => _mediaPlayer.Position = position, TimeSpan.FromMilliseconds(50));
            }
            else
            {
                _seekTimer.Stop();
                _mediaPlayer.Position = position;
            }
        }

        private void OnSourceChanged(IMediaPlayer sender, object? args)
        {
            _seekTimer.Stop();
            if (sender.Source == null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsSeekable = false;
                    Time = 0;
                    Chapters = sender.PlaybackItem?.Chapters;
                });
            }
            else
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Time = 0;
                });
            }
        }

        private void OnBufferingEnded(IMediaPlayer sender, object? args)
        {
            _bufferingTimer.Stop();
            _dispatcherQueue.TryEnqueue(() => BufferingVisible = false);
        }

        private void OnBufferingStarted(IMediaPlayer sender, object? args)
        {
            // When the player is paused, the following still triggers a buffering
            if (sender.Position == sender.NaturalDuration)
                return;

            // Only show buffering if it takes more than 0.5s
            _bufferingTimer.Debounce(() => BufferingVisible = true, TimeSpan.FromSeconds(0.5));
        }

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            if (_seekTimer.IsRunning || _timeChangeOverride) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                Time = sender.Position.TotalMilliseconds;
                if (!IsSeekable)
                {
                    IsSeekable = sender.CanSeek;
                }
            });
        }

        private void OnNaturalDurationChanged(IMediaPlayer sender, object? args)
        {
            // Natural duration can fluctuate during playback
            // Do not rely on this event to detect media changes
            _dispatcherQueue.TryEnqueue(() =>
            {
                Length = sender.NaturalDuration.TotalMilliseconds;
                IsSeekable = sender.CanSeek;
                Chapters = sender.PlaybackItem?.Chapters;
            });
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            if (!_timeChangeOverride)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    // Check if Time is close enough to Length. Sometimes a new file is already loaded at this point.
                    if (Length - Time is > 0 and < 400)
                    {
                        // Round Time to Length to avoid gap at the end
                        Time = Length;
                    }
                });
            }
        }
    }
}