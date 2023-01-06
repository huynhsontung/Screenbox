#nullable enable

using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Microsoft.Toolkit.Uwp.UI;

namespace Screenbox.ViewModels
{
    internal sealed partial class SeekBarViewModel :
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

        private IMediaPlayer? _mediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _seekTimer;
        private bool _timeChangeOverride;

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
            _mediaPlayer.NaturalDurationChanged += OnLengthChanged;
            _mediaPlayer.PositionChanged += OnTimeChanged;
            _mediaPlayer.MediaEnded += OnEndReached;
            _mediaPlayer.BufferingStarted += OnBufferingStarted;
            _mediaPlayer.BufferingEnded += OnBufferingEnded;
            _mediaPlayer.SourceChanged += OnSourceChanged;
        }

        private void OnSourceChanged(IMediaPlayer sender, object? args)
        {
            if (sender.Source == null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsSeekable = false;
                    Time = 0;
                    Length = 0;
                    Chapters = null;
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
            // Assume UI thread
            _timeChangeOverride = true;
            if (message.IsOffset)
            {
                Time += message.Value.TotalMilliseconds;
            }
            else
            {
                Time = message.Value.TotalMilliseconds;
            }

            _timeChangeOverride = false;
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
                double newTime = args.NewValue;
                bool paused = _mediaPlayer.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Buffering;
                if (args.OldValue == Time || paused || _timeChangeOverride)
                {
                    _seekTimer.Debounce(() => _mediaPlayer.Position = TimeSpan.FromMilliseconds(newTime),
                        TimeSpan.FromMilliseconds(50));
                }
            }
        }

        private void OnTimeChanged(IMediaPlayer sender, object? args)
        {
            if (!_timeChangeOverride)
            {
                if (_seekTimer.IsRunning) return;
                _dispatcherQueue.TryEnqueue(() => Time = sender.Position.TotalMilliseconds);
            }
        }

        private void OnLengthChanged(IMediaPlayer sender, object? args)
        {
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