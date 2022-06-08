#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.AppCenter.Utils.Synchronization;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Screenbox.Core;
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
        [ObservableProperty] private string? _titleName;
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private string _playPauseGlyph;

        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IMediaPlayerService _mediaPlayerService;

        public PlayerControlsViewModel(
            PlaylistViewModel playlistViewModel,
            IWindowService windowService,
            IMediaPlayerService mediaPlayerService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _windowService = windowService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.StateChanged += MediaPlayerServiceOnStateChanged;
            _mediaPlayerService.TitleChanged += OnTitleChanged;
            _playPauseGlyph = GetPlayPauseGlyph(false);
            PlaylistViewModel = playlistViewModel;
            PlaylistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
        }

        public string? GetChapterName(string? nullableName)
        {
            if (VlcPlayer is not { ChapterCount: > 1 }) return null;
            return string.IsNullOrEmpty(nullableName)
                ? Resources.ChapterName(VlcPlayer.Chapter + 1)
                : nullableName;
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

        private void UpdatePlayState(VLCState newState)
        {
            IsPlaying = newState == VLCState.Playing;
            PlayPauseGlyph = GetPlayPauseGlyph(IsPlaying);
        }

        private static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";
    }
}
