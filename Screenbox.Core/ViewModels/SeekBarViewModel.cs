#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using System;
using System.Collections.ObjectModel;
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

        [ObservableProperty] private double _previewTime;

        [ObservableProperty] private bool _shouldShowPreview;

        [ObservableProperty] private bool _shouldHandleKeyDown;

        public ObservableCollection<ChapterCue> Chapters { get; }

        private IMediaPlayer? _mediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _seekTimer;
        private readonly DispatcherQueueTimer _originalPositionTimer;
        private TimeSpan _originalPosition;
        private bool _timeChangeOverride;

        public SeekBarViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            _seekTimer = _dispatcherQueue.CreateTimer();
            _originalPositionTimer = _dispatcherQueue.CreateTimer();
            _originalPositionTimer.IsRepeating = false;
            _shouldShowPreview = true;
            _shouldHandleKeyDown = true;
            Chapters = new ObservableCollection<ChapterCue>();

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
            _mediaPlayer.PlaybackItemChanged += OnPlaybackItemChanged;
        }

        public void Receive(TimeChangeOverrideMessage message)
        {
            _timeChangeOverride = message.Value;
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            TimeSpan currentPosition = TimeSpan.FromMilliseconds(Time);
            _originalPositionTimer.Debounce(() => _originalPosition = currentPosition, TimeSpan.FromSeconds(1), true);

            // Assume UI thread
            Time = message.IsOffset
                ? Math.Clamp(Time + message.Value.TotalMilliseconds, 0, Length)
                : message.Value.TotalMilliseconds;
            SetPlayerPosition(TimeSpan.FromMilliseconds(Time), message.Debounce);

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
            // Only update player position when there is a user interaction.
            // SeekBar should have OneWay binding to Time, so when Time changes and invokes
            // this handler, Time = args.NewValue. The only exception is when the change is
            // coming from user.
            // We can detect user interaction by checking if Time != args.NewValue
            if (IsSeekable && _mediaPlayer != null && Math.Abs(Time - args.NewValue) > 50)
            {
                Time = args.NewValue;
                double currentMs = _mediaPlayer.Position.TotalMilliseconds;
                double newDiffMs = Math.Abs(args.NewValue - currentMs);
                bool shouldUpdate = newDiffMs > 400;
                bool shouldOverride = _timeChangeOverride && newDiffMs > 100;
                bool paused = _mediaPlayer.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Buffering;
                if (shouldUpdate || paused || shouldOverride)
                {
                    SetPlayerPosition(TimeSpan.FromMilliseconds(args.NewValue), true);
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

        private void OnPlaybackItemChanged(IMediaPlayer sender, object? args)
        {
            _seekTimer.Stop();
            if (sender.PlaybackItem == null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsSeekable = false;
                    Time = 0;
                    Chapters.Clear();
                });
            }
            else
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Time = 0;
                    Chapters.Clear();
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
                UpdateChapters(sender.PlaybackItem?.Chapters);
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

        private void UpdateChapters(PlaybackChapterList? chapterList)
        {
            Chapters.Clear();
            if (chapterList == null) return;
            if (_mediaPlayer != null)
            {
                chapterList.Load(_mediaPlayer);
            }

            foreach (ChapterCue chapterCue in chapterList)
            {
                Chapters.Add(chapterCue);
            }
            // Chapters.SyncItems(chapterList);
        }
    }
}