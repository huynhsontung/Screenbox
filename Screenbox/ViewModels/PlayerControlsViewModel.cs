#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;
using Screenbox.Core.Playback;
using Windows.UI.Xaml.Input;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlayerControlsViewModel : ObservableRecipient, IRecipient<MediaPlayerChangedMessage>
    {
        public MediaListViewModel Playlist { get; }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isCompact;
        [ObservableProperty] private bool _isFullscreen;
        [ObservableProperty] private bool _zoomToFit;
        [ObservableProperty] private string? _titleName;    // TODO: Handle VLC title name
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private string _playPauseGlyph;
        [ObservableProperty] private double _playbackSpeed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveSnapshotCommand))]
        private bool _hasVideo;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowPropertiesCommand))]
        [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleCompactLayoutCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleFullscreenCommand))]
        private bool _hasActiveItem;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IFilesService _filesService;
        private IMediaPlayer? _mediaPlayer;

        public PlayerControlsViewModel(
            MediaListViewModel playlist,
            IFilesService filesService,
            IWindowService windowService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _windowService = windowService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _playPauseGlyph = GetPlayPauseGlyph(false);
            _playbackSpeed = 1.0;
            Playlist = playlist;
            Playlist.PropertyChanged += PlaylistViewModelOnPropertyChanged;

            IsActive = true;
        }
        
        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _mediaPlayer.ChapterChanged += OnChapterChanged;
            _mediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        public void ToggleSubtitle(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_mediaPlayer?.PlaybackItem == null) return;
            PlaybackSubtitleTrackList subtitleTracks = _mediaPlayer.PlaybackItem.SubtitleTracks;
            if (subtitleTracks.Count == 0) return;
            args.Handled = true;
            switch (args.KeyboardAccelerator.Modifiers)
            {
                case VirtualKeyModifiers.None when subtitleTracks.Count == 1:
                    if (subtitleTracks.SelectedIndex >= 0)
                    {
                        subtitleTracks.SelectedIndex = -1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex = 0;
                        
                    }
                    break;

                case VirtualKeyModifiers.Control:
                    if (subtitleTracks.SelectedIndex == subtitleTracks.Count - 1)
                    {
                        subtitleTracks.SelectedIndex = -1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex++;
                    }
                    break;

                case VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift:
                    if (subtitleTracks.SelectedIndex == -1)
                    {
                        subtitleTracks.SelectedIndex = subtitleTracks.Count - 1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex--;
                    }
                    break;

                default:
                    args.Handled = false;
                    return;
            }

            Messenger.Send(subtitleTracks.SelectedIndex == -1
                ? new UpdateStatusMessage("Subtitle: None")
                : new UpdateStatusMessage($"Subtitle: {subtitleTracks[subtitleTracks.SelectedIndex].Label}"));
        }

        partial void OnZoomToFitChanged(bool value)
        {
            Messenger.Send(new ChangeZoomToFitMessage(value));
        }

        partial void OnPlaybackSpeedChanged(double value)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.PlaybackRate = value;
        }

        private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Playlist.CurrentItem):
                    HasActiveItem = Playlist.CurrentItem != null;
                    break;
            }
        }

        private void OnNaturalVideoSizeChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() => HasVideo = _mediaPlayer?.NaturalVideoHeight > 0);
            SaveSnapshotCommand.NotifyCanExecuteChanged();
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
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
        }

        [RelayCommand]
        private void SetPlaybackSpeed(string speedText)
        {
            PlaybackSpeed = double.Parse(speedText);
        }

        [RelayCommand(CanExecute = nameof(HasActiveItem))]
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

        [RelayCommand(CanExecute = nameof(HasActiveItem))]
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

        [RelayCommand(CanExecute = nameof(HasActiveItem))]
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

        [RelayCommand(CanExecute = nameof(HasVideo))]
        private async Task SaveSnapshotAsync()
        {
            if (_mediaPlayer?.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Playing)
            {
                try
                {
                    StorageFile file = await _filesService.SaveSnapshotAsync(_mediaPlayer);
                    Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
                }
                catch (Exception e)
                {
                    Messenger.Send(new ErrorMessage(Resources.FailedToSaveFrameNotificationTitle, e.ToString()));
                    // TODO: track error
                }
            }
        }

        [RelayCommand(CanExecute = nameof(HasActiveItem))]
        private async Task ShowPropertiesAsync()
        {
            if (Playlist.CurrentItem == null) return;
            ContentDialog propertiesDialog = PropertiesView.GetDialog(Playlist.CurrentItem);
            await propertiesDialog.ShowAsync();
        }

        private static string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";
    }
}
