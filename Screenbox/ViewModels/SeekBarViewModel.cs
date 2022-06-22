#nullable enable

using System;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Core.Playback;
using Microsoft.Toolkit.Uwp.UI;

namespace Screenbox.ViewModels
{
    internal partial class SeekBarViewModel :
        ObservableRecipient,
        IRecipient<TimeChangeOverrideMessage>,
        IRecipient<TimeRequestMessage>
    {
        [ObservableProperty] private double _length;

        [ObservableProperty] private double _time;

        [ObservableProperty] private bool _isSeekable;

        [ObservableProperty] private bool _bufferingVisible;

        [ObservableProperty] private ChapterDescription[] _chapters;

        private IMediaPlayer? _mediaPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private bool _timeChangeOverride;

        public SeekBarViewModel(LibVlcService libVlcService)
        {
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();
            libVlcService.Initialized += LibVlcService_Initialized;

            // Activate the view model's messenger
            IsActive = true;
        }

        private void LibVlcService_Initialized(LibVlcService sender, object? args)
        {
            if (sender.MediaPlayer == null) return;
            IMediaPlayer player = _mediaPlayer = sender.MediaPlayer;
            player.NaturalDurationChanged += OnLengthChanged;
            player.PositionChanged += OnTimeChanged;
            player.MediaEnded += OnEndReached;
            player.BufferingStarted += OnBufferingStarted;
            player.BufferingEnded += OnBufferingEnded;
        }

        private void OnBufferingEnded(IMediaPlayer sender, object? args)
        {
            _bufferingTimer.Stop();
            _dispatcherQueue.TryEnqueue(() => BufferingVisible = false);
        }

        private void OnBufferingStarted(IMediaPlayer sender, object? args)
        {
            // Only show buffering if it takes more than 0.5s
            _bufferingTimer.Debounce(() => BufferingVisible = true, TimeSpan.FromSeconds(0.5));
        }

        public void Receive(TimeChangeOverrideMessage message)
        {
            _timeChangeOverride = message.Value;
        }

        public void Receive(TimeRequestMessage message)
        {
            if (message.IsChangeRequest)
            {
                // Assume UI thread
                Time = message.Value.TotalMilliseconds;
            }

            message.Reply(TimeSpan.FromMilliseconds(Time));
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            _timeChangeOverride = pressed;
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (IsSeekable && _mediaPlayer != null)
            {
                double newTime = args.NewValue;
                bool isPlaying = _mediaPlayer.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing;
                if (args.OldValue == Time || !isPlaying || _timeChangeOverride)
                {
                    _mediaPlayer.Position = TimeSpan.FromMilliseconds(newTime);
                }
            }
        }

        private void OnTimeChanged(IMediaPlayer sender, object? args)
        {
            if (!_timeChangeOverride)
            {
                _dispatcherQueue.TryEnqueue(() => Time = sender.Position.TotalMilliseconds);
            }
        }

        private void OnLengthChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Time = 0;
                Length = sender.NaturalDuration.TotalMilliseconds;
                IsSeekable = sender.CanSeek;
                // TODO: Dont directly use Vlc media player to get chapter info
                VlcMediaPlayer mediaPlayer = (VlcMediaPlayer)sender;
                Chapters = mediaPlayer.VlcPlayer.FullChapterDescriptions();
            });
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            if (!_timeChangeOverride)
            {
                _dispatcherQueue.TryEnqueue(() => Time = Length);
            }
        }

        //private void OnStopped(object sender, EventArgs e)
        //{
        //    _dispatcherQueue.TryEnqueue(() =>
        //    {
        //        IsSeekable = false;
        //        Time = 0;
        //        Length = 0;
        //    });
        //}
    }
}
