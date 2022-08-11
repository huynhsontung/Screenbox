#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal partial class PlayerControlsViewModel : ObservableRecipient, IRecipient<MediaPlayerChangedMessage>
    {
        public PlaylistViewModel PlaylistViewModel { get; }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isCompact;
        [ObservableProperty] private bool _isFullscreen;
        [ObservableProperty] private bool _showPreviousNext;
        [ObservableProperty] private bool _zoomToFit;
        [ObservableProperty] private string? _titleName;    // TODO: Handle VLC title name
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private string _playPauseGlyph;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IFilesService _filesService;
        private IMediaPlayer? _mediaPlayer;

        public PlayerControlsViewModel(
            PlaylistViewModel playlistViewModel,
            IFilesService filesService,
            IWindowService windowService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _windowService = windowService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _playPauseGlyph = GetPlayPauseGlyph(false);
            PlaylistViewModel = playlistViewModel;
            PlaylistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
            PropertyChanged += OnPropertyChanged;

            IsActive = true;
        }
        
        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _mediaPlayer.ChapterChanged += OnChapterChanged;
        }

        public void SetPlaybackSpeed(string speedText)
        {
            if (_mediaPlayer == null) return;
            float.TryParse(speedText, out float speed);
            _mediaPlayer.PlaybackRate = speed;
        }

        private void UpdateShowPreviousNext()
        {
            ShowPreviousNext = PlaylistViewModel.CanSkip && !IsCompact;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ZoomToFit))
            {
                Messenger.Send(new ChangeZoomToFitMessage(ZoomToFit));
            }
        }

        private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaylistViewModel.CanSkip))
            {
                UpdateShowPreviousNext();
            }
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = sender.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing;
                PlayPauseGlyph = GetPlayPauseGlyph(IsPlaying);
            });
        }

        private void OnChapterChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ChapterName = sender.Chapter?.Title;
            });
        }

        private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
        {
            switch (e.NewValue)
            {
                case WindowViewMode.Default:
                    IsFullscreen = false;
                    IsCompact = false;
                    break;
                case WindowViewMode.Compact:
                    IsCompact = true;
                    IsFullscreen = false;
                    break;
                case WindowViewMode.FullScreen:
                    IsFullscreen = true;
                    IsCompact = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateShowPreviousNext();
        }

        [RelayCommand]
        private async Task ToggleCompactLayoutAsync()
        {
            if (IsCompact)
            {
                await _windowService.TryExitCompactLayoutAsync();
            }
            else if (_mediaPlayer?.NaturalVideoHeight > 0)
            {
                double aspectRatio = _mediaPlayer.NaturalVideoWidth / (double)_mediaPlayer.NaturalVideoHeight;
                await _windowService.TryEnterCompactLayoutAsync(new Size(240 * aspectRatio, 240));
            }
            else
            {
                await _windowService.TryEnterCompactLayoutAsync(new Size(240, 240));
            }
        }

        [RelayCommand]
        private void ToggleFullscreen()
        {
            if (IsCompact) return;
            if (IsFullscreen)
            {
                _windowService.ExitFullScreen();
            }
            else
            {
                _windowService.TryEnterFullScreen();
            }
        }

        [RelayCommand]
        private void PlayPause()
        {
            if (IsPlaying)
            {
                _mediaPlayer?.Pause();
            }
            else
            {
                _mediaPlayer?.Play();
            }
        }

        [RelayCommand]
        private async Task SaveSnapshotAsync()
        {
            if (_mediaPlayer?.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Playing)
            {
                try
                {
                    StorageFile file = await _filesService.SaveSnapshot(_mediaPlayer);
                    Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
                }
                catch (Exception e)
                {
                    Messenger.Send(new ErrorMessage(Resources.FailedToSaveFrameNotificationTitle, e.ToString()));
                    // TODO: track error
                }
            }
        }

        private static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";
    }
}
