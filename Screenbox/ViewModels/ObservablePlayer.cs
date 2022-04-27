#nullable enable

using System;
using Windows.System;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    public partial class ObservablePlayer : ObservableObject
    {
        public MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        public bool ShouldUpdateTime { get; set; }

        [ObservableProperty]
        private double _length;

        [ObservableProperty]
        private double _time;

        [ObservableProperty]
        private bool _isSeekable;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private VLCState _state;

        [ObservableProperty]
        private bool _shouldLoop;

        [ObservableProperty]
        private double _bufferingProgress;

        [ObservableProperty]
        private ChapterDescription[] _chapters;

        [ObservableProperty]
        private ChapterDescription _currentChapter;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IMediaPlayerService _mediaPlayerService;

        public ObservablePlayer(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _mediaPlayerService.VlcPlayerChanged += MediaPlayerServiceOnVlcPlayerChanged;
            _chapters = Array.Empty<ChapterDescription>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ShouldUpdateTime = true;
            _bufferingProgress = 100;
            _state = VLCState.NothingSpecial;
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

        private void MediaPlayerServiceOnVlcPlayerChanged(object sender, ValueChangedEventArgs<MediaPlayer?> e)
        {
            if (e.OldValue != null)
            {
                RemoveMediaPlayerEventHandlers(e.OldValue);
            }

            if (e.NewValue != null)
            {
                RegisterMediaPlayerEventHandlers(e.NewValue);
            }
        }

        private void RegisterMediaPlayerEventHandlers(MediaPlayer vlcPlayer)
        {
            vlcPlayer.LengthChanged += OnLengthChanged;
            vlcPlayer.TimeChanged += OnTimeChanged;
            vlcPlayer.SeekableChanged += OnSeekableChanged;
            vlcPlayer.EndReached += OnEndReached;
            vlcPlayer.Playing += OnStateChanged;
            vlcPlayer.Paused += OnStateChanged;
            vlcPlayer.Stopped += OnStateChanged;
            vlcPlayer.EncounteredError += OnStateChanged;
            vlcPlayer.Opening += OnStateChanged;
            vlcPlayer.Buffering += OnBuffering;
            vlcPlayer.ChapterChanged += OnChapterChanged;
        }

        private void RemoveMediaPlayerEventHandlers(MediaPlayer vlcPlayer)
        {
            vlcPlayer.LengthChanged -= OnLengthChanged;
            vlcPlayer.TimeChanged -= OnTimeChanged;
            vlcPlayer.SeekableChanged -= OnSeekableChanged;
            vlcPlayer.EndReached -= OnEndReached;
            vlcPlayer.Playing -= OnStateChanged;
            vlcPlayer.Paused -= OnStateChanged;
            vlcPlayer.Stopped -= OnStateChanged;
            vlcPlayer.EncounteredError -= OnStateChanged;
            vlcPlayer.Opening -= OnStateChanged;
            vlcPlayer.Buffering -= OnBuffering;
            vlcPlayer.ChapterChanged -= OnChapterChanged;
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => BufferingProgress = e.Cache);
        }

        private void UpdateState()
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                State = VlcPlayer.State;
                IsPlaying = VlcPlayer.IsPlaying;
            });
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            UpdateState();
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

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));

            if (ShouldLoop)
            {
                _dispatcherQueue.TryEnqueue(_mediaPlayerService.Replay);
                return;
            }

            if (ShouldUpdateTime)
            {
                _dispatcherQueue.TryEnqueue(() => Time = VlcPlayer.Length);
            }

            UpdateState();
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
    }
}
