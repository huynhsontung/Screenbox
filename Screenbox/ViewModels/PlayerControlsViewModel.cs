#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal partial class PlayerControlsViewModel : ObservableRecipient
    {
        public PlaylistViewModel PlaylistViewModel { get; }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isCompact;
        [ObservableProperty] private bool _isFullscreen;
        [ObservableProperty] private bool _showPreviousNext;
        [ObservableProperty] private bool _zoomToFit;
        [ObservableProperty] private string? _titleName;
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private string _playPauseGlyph;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly IFilesService _filesService;

        public PlayerControlsViewModel(
            PlaylistViewModel playlistViewModel,
            IFilesService filesService,
            IWindowService windowService,
            IMediaPlayerService mediaPlayerService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _windowService = windowService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.StateChanged += MediaPlayerServiceOnStateChanged;
            _mediaPlayerService.TitleChanged += OnTitleChanged;
            _playPauseGlyph = GetPlayPauseGlyph(false);
            PlaylistViewModel = playlistViewModel;
            PlaylistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
            PropertyChanged += OnPropertyChanged;
        }

        public string? GetChapterName(string? nullableName)
        {
            if (VlcPlayer is not { ChapterCount: > 1 }) return null;
            return string.IsNullOrEmpty(nullableName)
                ? Resources.ChapterName(VlcPlayer.Chapter + 1)
                : nullableName;
        }

        public void SetPlaybackSpeed(string speedText)
        {
            float.TryParse(speedText, out float speed);
            _mediaPlayerService.Rate = speed;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ZoomToFit))
            {
                Messenger.Send(new ZoomToFitChangedMessage(ZoomToFit));
            }
        }

        private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaylistViewModel.CanSkip))
            {
                ShowPreviousNext = PlaylistViewModel.CanSkip && !IsCompact;
            }
        }

        private void MediaPlayerServiceOnStateChanged(object sender, PlayerStateChangedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => UpdatePlayState(e.NewValue));
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
        }

        private void OnTitleChanged(object sender, MediaPlayerTitleChangedEventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                TitleName = VlcPlayer.TitleDescription.FirstOrDefault(title => title.Id == e.Title).Name;
            });
        }

        [ICommand]
        private async Task ToggleCompactLayout()
        {
            if (IsCompact)
            {
                await _windowService.TryExitCompactLayoutAsync();
            }
            else
            {
                await _windowService.TryEnterCompactLayoutAsync(new Size(240 * (_mediaPlayerService.NumericAspectRatio ?? 1), 240));
            }
        }

        [ICommand]
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

        [ICommand]
        private void PlayPause()
        {
            if (_mediaPlayerService.State == VLCState.Ended)
            {
                _mediaPlayerService.Replay();
                return;
            }

            _mediaPlayerService.Pause();
        }


        [ICommand]
        private async Task SaveSnapshot()
        {
            if (VlcPlayer == null || !VlcPlayer.WillPlay) return;
            try
            {
                StorageFile file = await _filesService.SaveSnapshot(VlcPlayer);
                Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(Resources.FailedToSaveFrameNotificationTitle, e.ToString()));
                // TODO: track error
            }
        }

        private void UpdatePlayState(VLCState newState)
        {
            IsPlaying = newState == VLCState.Playing;
            PlayPauseGlyph = GetPlayPauseGlyph(IsPlaying);
        }

        private static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";
    }
}
