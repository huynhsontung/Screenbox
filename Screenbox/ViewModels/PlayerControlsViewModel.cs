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
using Screenbox.Core.Playback;
using LibVLCSharp.Shared.Structures;

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
        [ObservableProperty] private ChapterDescription _currentChapter;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IFilesService _filesService;
        private IMediaPlayer? _mediaPlayer;
        private MediaPlayer? _vlcPlayer;

        public PlayerControlsViewModel(
            PlaylistViewModel playlistViewModel,
            LibVlcService libVlcService,
            IFilesService filesService,
            IWindowService windowService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _windowService = windowService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            libVlcService.Initialized += LibVlcService_Initialized;
            _playPauseGlyph = GetPlayPauseGlyph(false);
            PlaylistViewModel = playlistViewModel;
            PlaylistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
            PropertyChanged += OnPropertyChanged;
        }

        public string? GetChapterName(string? nullableName)
        {
            if (_vlcPlayer is not { ChapterCount: > 1 }) return null;
            return string.IsNullOrEmpty(nullableName)
                ? Resources.ChapterName(_vlcPlayer.Chapter + 1)
                : nullableName;
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

        private void LibVlcService_Initialized(LibVlcService sender, MediaPlayerInitializedEventArgs args)
        {
            _mediaPlayer = args.MediaPlayer;
            VlcMediaPlayer player = (VlcMediaPlayer)args.MediaPlayer;
            _vlcPlayer = player.VlcPlayer;
            player.PlaybackStateChanged += OnPlaybackStateChanged;
            player.VlcPlayer.TitleChanged += OnTitleChanged;
        }

        private void OnChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            Guard.IsNotNull(_vlcPlayer, nameof(_vlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                var chapters = _vlcPlayer.FullChapterDescriptions();
                if (chapters.Length == 0) return;
                CurrentChapter = chapters[e.Chapter];
            });
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

        private void OnTitleChanged(object sender, MediaPlayerTitleChangedEventArgs e)
        {
            Guard.IsNotNull(_vlcPlayer, nameof(_vlcPlayer));
            _dispatcherQueue.TryEnqueue(() =>
            {
                TitleName = _vlcPlayer.TitleDescription.FirstOrDefault(title => title.Id == e.Title).Name;
            });
        }

        [ICommand]
        private async Task ToggleCompactLayout()
        {
            if (IsCompact)
            {
                await _windowService.TryExitCompactLayoutAsync();
            }
            else if (_mediaPlayer?.NaturalVideoHeight > 0)
            {
                double aspectRatio = _mediaPlayer.NaturalVideoWidth / _mediaPlayer.NaturalVideoHeight;
                await _windowService.TryEnterCompactLayoutAsync(new Size(240 * aspectRatio, 240));
            }
            else
            {
                await _windowService.TryEnterCompactLayoutAsync(new Size(240, 240));
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
            if (IsPlaying)
            {
                _mediaPlayer?.Pause();
            }
            else
            {
                _mediaPlayer?.Play();
            }
        }


        [ICommand]
        private async Task SaveSnapshot()
        {
            if (_vlcPlayer == null || !_vlcPlayer.WillPlay) return;
            try
            {
                StorageFile file = await _filesService.SaveSnapshot(_vlcPlayer);
                Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(Resources.FailedToSaveFrameNotificationTitle, e.ToString()));
                // TODO: track error
            }
        }

        private static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";
    }
}
