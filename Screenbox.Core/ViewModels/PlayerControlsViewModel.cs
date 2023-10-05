#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PlayerControlsViewModel : ObservableRecipient,
        IRecipient<MediaPlayerChangedMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<TogglePlayPauseMessage>
    {
        public MediaListViewModel Playlist { get; }

        public bool ShouldBeAdaptive => !IsCompact && SystemInformationExtensions.IsDesktop;

        public long SubtitleTimingOffset
        {
            // Special access. Consider promote to proper IMediaPlayer property
            get => (_mediaPlayer as VlcMediaPlayer)?.VlcPlayer.SpuDelay ?? 0;
            set
            {
                if (_mediaPlayer is VlcMediaPlayer player)
                {
                    player.VlcPlayer.SetSpuDelay(value);
                }
            }
        }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isFullscreen;
        [ObservableProperty] private string? _titleName; // TODO: Handle VLC title name
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private double _playbackSpeed;
        [ObservableProperty] private bool _isAdvancedModeActive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldBeAdaptive))]
        private bool _isCompact;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveSnapshotCommand))]
        private bool _hasVideo;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleCompactLayoutCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleFullscreenCommand))]
        private bool _hasActiveItem;


        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private readonly ISettingsService _settingsService;
        private IMediaPlayer? _mediaPlayer;
        private Size _aspectRatio;

        public PlayerControlsViewModel(
            MediaListViewModel playlist,
            IFilesService filesService,
            ISettingsService settingsService,
            IWindowService windowService,
            IResourceService resourceService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _filesService = filesService;
            _windowService = windowService;
            _resourceService = resourceService;
            _settingsService = settingsService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _playbackSpeed = 1.0;
            _isAdvancedModeActive = settingsService.AdvancedMode;
            Playlist = playlist;
            Playlist.PropertyChanged += PlaylistViewModelOnPropertyChanged;

            IsActive = true;
        }

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName != nameof(SettingsPageViewModel.AdvancedMode)) return;
            IsAdvancedModeActive = _settingsService.AdvancedMode;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            _mediaPlayer = message.Value;
            _mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            _mediaPlayer.ChapterChanged += OnChapterChanged;
            _mediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        public void Receive(TogglePlayPauseMessage message)
        {
            if (!HasActiveItem || _mediaPlayer == null) return;
            if (message.ShowBadge)
            {
                PlayPauseWithBadge();
            }
            else
            {
                PlayPause();
            }

        }

        public void PlayPauseKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            PlayerVisibilityState playerVisibility = Messenger.Send(new PlayerVisibilityRequestMessage());
            if (args.KeyboardAccelerator.Key == VirtualKey.Space &&
                playerVisibility != PlayerVisibilityState.Visible) return;

            // Override default keyboard accelerator to show badge
            args.Handled = true;
            PlayPauseWithBadge();
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

            string status = subtitleTracks.SelectedIndex == -1
                ? _resourceService.GetString(ResourceName.SubtitleStatus, _resourceService.GetString(ResourceName.None))
                : _resourceService.GetString(ResourceName.SubtitleStatus,
                    subtitleTracks[subtitleTracks.SelectedIndex].Label);

            Messenger.Send(new UpdateStatusMessage(status));
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
                case nameof(MediaListViewModel.CurrentItem):
                    HasActiveItem = Playlist.CurrentItem != null;
                    SubtitleTimingOffset = 0;
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
                if (Playlist.CurrentItem != null)
                {
                    Playlist.CurrentItem.IsPlaying = IsPlaying;
                }
            });
        }

        private void OnChapterChanged(IMediaPlayer sender, object? args)
        {
            _dispatcherQueue.TryEnqueue(() => ChapterName = sender.Chapter?.Title);
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
        private void ResetMediaPlayback()
        {
            if (_mediaPlayer == null) return;
            TimeSpan pos = _mediaPlayer.Position;
            MediaViewModel? item = Playlist.CurrentItem;
            Playlist.CurrentItem = null;
            Playlist.CurrentItem = item;
            _dispatcherQueue.TryEnqueue(() =>
            {
                _mediaPlayer.Play();
                _mediaPlayer.Position = pos;
            });
        }

        [RelayCommand]
        private void SetPlaybackSpeed(string speedText)
        {
            if (!double.TryParse(speedText, out double speed)) return;
            PlaybackSpeed = speed;
        }

        [RelayCommand]
        private void SetAspectRatio(string aspect)
        {
            switch (aspect)
            {
                case "Fit":
                    _aspectRatio = new Size(0, 0);
                    break;
                case "Fill":
                    _aspectRatio = new Size(double.NaN, double.NaN);
                    break;
                default:
                    string[] values = aspect.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length != 2) return;
                    if (!double.TryParse(values[0], out double width)) return;
                    if (!double.TryParse(values[1], out double height)) return;
                    _aspectRatio = new Size(width, height);
                    break;
            }

            Messenger.Send(new ChangeAspectRatioMessage(_aspectRatio));
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
                catch (UnauthorizedAccessException)
                {
                    Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Pictures));
                }
                catch (Exception e)
                {
                    Messenger.Send(new ErrorMessage(
                        _resourceService.GetString(ResourceName.FailedToSaveFrameNotificationTitle), e.ToString()));
                    // TODO: track error
                }
            }
        }

        private void PlayPauseWithBadge()
        {
            if (!HasActiveItem) return;
            Messenger.Send(new ShowPlayPauseBadgeMessage(!IsPlaying));
            PlayPause();
        }
    }
}