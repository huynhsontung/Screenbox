#nullable enable

using System;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    public partial class ObservablePlayer : ObservableObject
    {
        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private VLCState _state;

        [ObservableProperty]
        private bool _shouldLoop;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IMediaPlayerService _mediaPlayerService;

        public ObservablePlayer(IMediaPlayerService mediaPlayer)
        {
            _mediaPlayerService = mediaPlayer;
            _mediaPlayerService.VlcPlayerChanged += MediaPlayerServiceOnVlcPlayerChanged;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _state = VLCState.NothingSpecial;
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
            vlcPlayer.EndReached += OnEndReached;
            vlcPlayer.Playing += OnStateChanged;
            vlcPlayer.Paused += OnStateChanged;
            vlcPlayer.Stopped += OnStateChanged;
            vlcPlayer.EncounteredError += OnStateChanged;
            vlcPlayer.Opening += OnStateChanged;
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

        private void OnEndReached(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            if (ShouldLoop)
            {
                _dispatcherQueue.TryEnqueue(_mediaPlayerService.Replay);
                return;
            }

            UpdateState();
        }
    }
}
