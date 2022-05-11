#nullable enable

using System;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class SeekBarViewModel :
        ObservableRecipient,
        IRecipient<SeekBarInteractionRequestMessage>,
        IRecipient<TimeRequestMessage>
    {
        [ObservableProperty] private double _length;

        [ObservableProperty] private double _time;

        [ObservableProperty] private bool _isSeekable;

        [ObservableProperty] private bool _bufferingVisible;

        [ObservableProperty] private ChapterDescription[] _chapters;

        [ObservableProperty] private ChapterDescription _currentChapter;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly IMediaPlayerService _mediaPlayerService;
        private bool _shouldUpdateTime;

        public SeekBarViewModel(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();

            _mediaPlayerService.LengthChanged += OnLengthChanged;
            _mediaPlayerService.TimeChanged += OnTimeChanged;
            _mediaPlayerService.SeekableChanged += OnSeekableChanged;
            _mediaPlayerService.EndReached += OnEndReached;
            _mediaPlayerService.Buffering += OnBuffering;
            _mediaPlayerService.ChapterChanged += OnChapterChanged;

            _shouldUpdateTime = true;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(SeekBarInteractionRequestMessage message)
        {
            if (message.IsChangeRequest)
            {
                _shouldUpdateTime = !message.Value;
            }

            message.Reply(!_shouldUpdateTime);
        }

        public void Receive(TimeRequestMessage message)
        {
            if (message.IsChangeRequest)
            {
                // Assume UI thread
                Time = message.Value;
            }

            message.Reply(Time);
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            _shouldUpdateTime = !pressed;
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (IsSeekable && VlcPlayer != null)
            {
                double newTime = args.NewValue;
                if ((args.OldValue == Time || !VlcPlayer.IsPlaying || !_shouldUpdateTime) && newTime != Length)
                {
                    _mediaPlayerService.SetTime(newTime);
                }
            }
        }

        private void OnChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var chapters = Chapters;
                if (chapters.Length == 0) return;
                CurrentChapter = chapters[e.Chapter];
            });
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            // Only show buffering if it takes more than 0.5s
            _bufferingTimer.Debounce(() => BufferingVisible = e.Cache < 100, TimeSpan.FromSeconds(0.5));
        }

        private void OnSeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => IsSeekable = e.Seekable != 0);
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (_shouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = e.Time);
            }
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                Time = 0;
                Length = e.Length;
                Chapters = VlcPlayer.FullChapterDescriptions();
                CurrentChapter = Chapters.Length > 0 ? Chapters[VlcPlayer.Chapter] : default;
            });
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            if (_shouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = VlcPlayer.Length);
            }
        }
    }
}
