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
        IRecipient<ChangeSeekBarInteractionRequestMessage>,
        IRecipient<ChangeTimeRequestMessage>
    {
        [ObservableProperty] private double _length;

        [ObservableProperty] private double _time;

        [ObservableProperty] private bool _isSeekable;

        [ObservableProperty] private bool _bufferingVisible;

        [ObservableProperty] private ChapterDescription[] _chapters;

        [ObservableProperty] private ChapterDescription _currentChapter;

        private bool ShouldUpdateTime { get; set; }

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _bufferingTimer;
        private readonly IMediaPlayerService _mediaPlayerService;

        public SeekBarViewModel(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _mediaPlayerService.VlcPlayerChanged += MediaPlayerServiceOnVlcPlayerChanged;
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _bufferingTimer = _dispatcherQueue.CreateTimer();

            ShouldUpdateTime = true;

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(ChangeSeekBarInteractionRequestMessage message)
        {
            if (message.Value is bool value)
            {
                ShouldUpdateTime = !value;
            }

            message.Reply(!ShouldUpdateTime);
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            if (message.Value is double value)
            {
                // Assume UI thread
                Time = value;
            }

            message.Reply(Time);
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            ShouldUpdateTime = !pressed;
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            if (IsSeekable && VlcPlayer != null)
            {
                double newTime = args.NewValue;
                if (args.OldValue == Time || !VlcPlayer.IsPlaying || !ShouldUpdateTime && newTime != Length)
                {
                    _mediaPlayerService.SetTime(newTime);
                }
            }
        }

        private void MediaPlayerServiceOnVlcPlayerChanged(object sender, EventArgs e)
        {
            if (_mediaPlayerService.VlcPlayer != null)
            {
                RegisterMediaPlayerEventHandlers(_mediaPlayerService.VlcPlayer);
            }
        }

        private void RegisterMediaPlayerEventHandlers(MediaPlayer vlcPlayer)
        {
            vlcPlayer.LengthChanged += OnLengthChanged;
            vlcPlayer.TimeChanged += OnTimeChanged;
            vlcPlayer.SeekableChanged += OnSeekableChanged;
            vlcPlayer.EndReached += OnEndReached;
            vlcPlayer.Buffering += OnBuffering;
            vlcPlayer.ChapterChanged += OnChapterChanged;
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
            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = e.Time);
            }
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                Length = e.Length;
                Chapters = VlcPlayer.FullChapterDescriptions();
                CurrentChapter = Chapters.Length > 0 ? Chapters[VlcPlayer.Chapter] : default;
            });
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = VlcPlayer.Length);
            }
        }
    }
}
