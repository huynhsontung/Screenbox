#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class SeekBarViewModel :
        ObservableRecipient,
        IRecipient<TimeChangeOverrideMessage>,
        IRecipient<ChangeTimeRequestMessage>,
        IRecipient<PlayerControlsVisibilityChangedMessage>,
        IRecipient<PlaylistCurrentItemChangedMessage>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>,
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

        private TimeSpan NaturalDuration => TimeSpan.FromMilliseconds(Length);

        private TimeSpan Position
        {
            get => TimeSpan.FromMilliseconds(Time);
            set => Time = value.TotalMilliseconds;
        }

        private IMediaPlayer? _mediaPlayer;

        private readonly ISettingsService _settingsService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly DispatcherQueueTimer _seekTimer;
        private readonly DispatcherQueueTimer _originalPositionTimer;
        private readonly LastPositionTracker _lastPositionTracker;
        private TimeSpan _originalPosition;
        private TimeSpan _lastTrackedPosition;
        private bool _timeChangeOverride;
        private MediaViewModel? _currentItem;

        public SeekBarViewModel(ISettingsService settingsService, LastPositionTracker lastPositionTracker)
        {
            _settingsService = settingsService;
            _lastPositionTracker = lastPositionTracker;
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

        public void Receive(PlaylistCurrentItemChangedMessage message)
        {
            _lastTrackedPosition = TimeSpan.Zero;
            _currentItem = message.Value;
            if (message.Value != null && _lastPositionTracker.IsLoaded)
            {
                RestoreLastPosition(message.Value);
            }
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            ShouldHandleKeyDown = message.NewValue != PlayerVisibilityState.Visible;
        }

        public void Receive(PlayerControlsVisibilityChangedMessage message)
        {
            if (!message.Value && ShouldShowPreview)
            {
                ShouldShowPreview = false;
            }
        }

        public async void Receive(MediaPlayerChangedMessage message)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
                _mediaPlayer.NaturalDurationChanged -= OnNaturalDurationChanged;
                _mediaPlayer.PositionChanged -= OnPositionChanged;
                _mediaPlayer.MediaEnded -= OnEndReached;
                _mediaPlayer.BufferingStarted -= OnBufferingStarted;
                _mediaPlayer.BufferingEnded -= OnBufferingEnded;
                _mediaPlayer.PlaybackItemChanged -= OnPlaybackItemChanged;
                _mediaPlayer.CanSeekChanged -= OnCanSeekChanged;
            }

            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _mediaPlayer.NaturalDurationChanged += OnNaturalDurationChanged;
            _mediaPlayer.PositionChanged += OnPositionChanged;
            _mediaPlayer.MediaEnded += OnEndReached;
            _mediaPlayer.BufferingStarted += OnBufferingStarted;
            _mediaPlayer.BufferingEnded += OnBufferingEnded;
            _mediaPlayer.PlaybackItemChanged += OnPlaybackItemChanged;
            _mediaPlayer.CanSeekChanged += OnCanSeekChanged;

            if (!_lastPositionTracker.IsLoaded)
            {
                await _lastPositionTracker.LoadFromDiskAsync();
                if (_currentItem != null)
                {
                    RestoreLastPosition(_currentItem);
                }
            }
        }

        public void Receive(TimeChangeOverrideMessage message)
        {
            _timeChangeOverride = message.Value;
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            var result = UpdatePosition(message.Value, message.IsOffset, message.Debounce);
            message.Reply(result);
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

        public void OnSeekBarPointerWheelChanged(double pointerWheelDelta)
        {
            if (!IsSeekable || _mediaPlayer == null) return;
            var controlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down;
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down;
            var delta = 5000;
            if (controlPressed) delta = 10000;
            if (shiftPressed) delta = 2000;
            var result = UpdatePosition(TimeSpan.FromMilliseconds(pointerWheelDelta > 0 ? delta : -delta), true, true);
            TimeSpan offset = result.NewPosition - result.OriginalPosition;
            string extra = $"{(offset > TimeSpan.Zero ? '+' : string.Empty)}{Humanizer.ToDuration(offset)}";
            Messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            var newPosition = TimeSpan.FromMilliseconds(args.NewValue);
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
                    SetPlayerPosition(newPosition, true);
                }
            }

            UpdateLastPosition(newPosition);
        }

        private void RestoreLastPosition(MediaViewModel media)
        {
            TimeSpan lastPosition = _lastPositionTracker.GetPosition(media.Location);
            if (lastPosition <= TimeSpan.Zero) return;
            if (_settingsService.RestorePlaybackPosition)
            {
                if (_mediaPlayer?.PlaybackState is { } and not (MediaPlaybackState.None or MediaPlaybackState.Opening))
                {
                    UpdatePosition(lastPosition, false, false);
                }
                else
                {
                    // Media is not seekable yet, so we need to wait for the PlaybackStateChanged event
                    _lastTrackedPosition = lastPosition;
                }
            }
            else
            {
                Messenger.Send(new RaiseResumePositionNotificationMessage(lastPosition));
            }
        }

        private PositionChangedResult UpdatePosition(TimeSpan position, bool isOffset, bool debounce)
        {
            TimeSpan currentPosition = Position;
            _originalPositionTimer.Debounce(() => _originalPosition = currentPosition, TimeSpan.FromSeconds(1), true);

            // Assume UI thread
            Position = isOffset ? (currentPosition + position) switch
            {
                var newPosition when newPosition < TimeSpan.Zero => TimeSpan.Zero,
                var newPosition when newPosition > NaturalDuration => NaturalDuration,
                var newPosition => newPosition
            } : position;
            SetPlayerPosition(Position, debounce);

            return new PositionChangedResult(currentPosition, Position, _originalPosition, NaturalDuration);
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

        private void OnCanSeekChanged(IMediaPlayer sender, EventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsSeekable = sender.CanSeek;
            });
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, ValueChangedEventArgs<MediaPlaybackState> args)
        {
            if (args.NewValue is not (MediaPlaybackState.None or MediaPlaybackState.Opening) &&
                _lastTrackedPosition > TimeSpan.Zero)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    SetPlayerPosition(_lastTrackedPosition, false);
                    _lastTrackedPosition = TimeSpan.Zero;
                });
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

        private void UpdateLastPosition(TimeSpan position)
        {
            if (_currentItem == null || NaturalDuration <= TimeSpan.FromMinutes(1) ||
                DateTimeOffset.Now - _lastPositionTracker.LastUpdated <= TimeSpan.FromSeconds(3))
                return;

            if (position > TimeSpan.FromSeconds(30) && position + TimeSpan.FromSeconds(10) < NaturalDuration)
            {
                _lastPositionTracker.UpdateLastPosition(_currentItem.Location, position);
            }
            else if (position > TimeSpan.FromSeconds(5))
            {
                _lastPositionTracker.RemovePosition(_currentItem.Location);
            }
        }
    }
}