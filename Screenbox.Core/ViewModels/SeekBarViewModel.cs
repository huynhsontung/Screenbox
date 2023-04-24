#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
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
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private double _length;

        [ObservableProperty] private double _time;

        [ObservableProperty] private bool _isSeekable;

        [ObservableProperty] private bool _bufferingVisible;

        [ObservableProperty] private IReadOnlyCollection<ChapterCue>? _chapters;

        [ObservableProperty] private double _previewTime;

        [ObservableProperty] private bool _shouldShowPreview;

        private IMediaPlayer? _mediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _seekTimer;
        private bool _timeChangeOverride;
        private bool _debounceOverride;

        public SeekBarViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            _seekTimer = _dispatcherQueue.CreateTimer();

            // Activate the view model's messenger
            IsActive = true;
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

        public void Receive(TimeChangeOverrideMessage message)
        {
            _timeChangeOverride = message.Value;
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            if (!message.Debounce)
                _debounceOverride = true;

            // Assume UI thread
            Time = message.IsOffset
                ? Math.Clamp(Time + message.Value.TotalMilliseconds, 0, Length)
                : message.Value.TotalMilliseconds;

            message.Reply(TimeSpan.FromMilliseconds(Time));
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
                if (_debounceOverride)
                {
                    _debounceOverride = false;
                    _seekTimer.Stop();
                    _mediaPlayer.Position = TimeSpan.FromMilliseconds(args.NewValue);
                }
                else if (shouldUpdate || paused || shouldOverride)
                {
                    _seekTimer.Debounce(() => _mediaPlayer.Position = TimeSpan.FromMilliseconds(args.NewValue),
                        TimeSpan.FromMilliseconds(50));
                }
            }
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
                _dispatcherQueue.TryEnqueue(() => Time = Length);
            }
        }
    }
}