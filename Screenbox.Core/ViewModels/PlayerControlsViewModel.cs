﻿#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.ComponentModel;
using System.Globalization;
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
        IRecipient<TogglePlayPauseMessage>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
    {
        public MediaListViewModel Playlist { get; }

        public bool ShouldBeAdaptive => !IsCompact && SystemInformation.IsDesktop;

        public double SubtitleTimingOffset
        {
            // Special access. Consider promote to proper IMediaPlayer property
            get => _subtitleTimingOffset = (_mediaPlayer as VlcMediaPlayer)?.VlcPlayer.SpuDelay / 1000 ?? 0;
            set
            {
                if (_mediaPlayer is VlcMediaPlayer player)
                {
                    // LibVLC subtitle delay is in microseconds, convert to milliseconds with multiplication by 1000
                    player.VlcPlayer.SetSpuDelay((long)(value * 1000));
                    SetProperty(ref _subtitleTimingOffset, value);
                }
            }
        }

        /// <summary>
        /// This 64-bit signed integer changes the current audio delay.
        /// </summary>
        public double AudioTimingOffset
        {
            // Special access. Consider promote to proper IMediaPlayer property
            get => _audioTimingOffset = (_mediaPlayer as VlcMediaPlayer)?.VlcPlayer.AudioDelay / 1000 ?? 0;
            set
            {
                if (_mediaPlayer is VlcMediaPlayer player)
                {
                    // LibVLC audio delay is in microseconds, convert to milliseconds with multiplication by 1000
                    player.VlcPlayer.SetAudioDelay((long)(value * 1000));
                    SetProperty(ref _audioTimingOffset, value);
                }
            }
        }

        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isFullscreen;
        [ObservableProperty] private string? _titleName; // TODO: Handle VLC title name
        [ObservableProperty] private string? _chapterName;
        [ObservableProperty] private double _playbackSpeed;
        [ObservableProperty] private bool _isAdvancedModeActive;
        [ObservableProperty] private bool _isMinimal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldBeAdaptive))]
        private bool _isCompact;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveSnapshotCommand))]
        private bool _hasVideo;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
        private bool _hasActiveItem;


        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowService _windowService;
        private readonly IResourceService _resourceService;
        private readonly ISettingsService _settingsService;
        private IMediaPlayer? _mediaPlayer;
        private Size _aspectRatio;
        private double _subtitleTimingOffset;
        private double _audioTimingOffset;

        public PlayerControlsViewModel(
            MediaListViewModel playlist,
            ISettingsService settingsService,
            IWindowService windowService,
            IResourceService resourceService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _windowService = windowService;
            _resourceService = resourceService;
            _settingsService = settingsService;
            _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            _playbackSpeed = 1.0;
            _isAdvancedModeActive = settingsService.AdvancedMode;
            _isMinimal = true;
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

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            IsMinimal = message.NewValue != PlayerVisibilityState.Visible;
        }

        public void PlayPauseKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (args.KeyboardAccelerator.Key == VirtualKey.Space && IsMinimal) return;

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
                    AudioTimingOffset = 0;
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
        private void SetPlaybackSpeed(double speed)
        {
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
                    if (!double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double width)) return;
                    if (!double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double height)) return;
                    _aspectRatio = new Size(width, height);
                    break;
            }

            Messenger.Send(new ChangeAspectRatioMessage(_aspectRatio));
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
                    StorageFile file = await SaveSnapshotInternalAsync(_mediaPlayer);
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

        private static async Task<StorageFile> SaveSnapshotInternalAsync(IMediaPlayer mediaPlayer)
        {
            if (mediaPlayer is not VlcMediaPlayer player)
            {
                throw new NotImplementedException("Not supported on non VLC players");
            }

            StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                $"snapshot_{DateTimeOffset.Now.Ticks}",
                CreationCollisionOption.FailIfExists);

            try
            {
                if (!player.VlcPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                    throw new Exception("VLC failed to save snapshot");

                StorageFile file = (await tempFolder.GetFilesAsync())[0];
                StorageLibrary pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                StorageFolder defaultSaveFolder = pictureLibrary.SaveFolder;
                StorageFolder destFolder =
                    await defaultSaveFolder.CreateFolderAsync("Screenbox",
                        CreationCollisionOption.OpenIfExists);
                return await file.CopyAsync(destFolder, $"Screenbox_{DateTime.Now:yyyymmdd_HHmmss}{file.FileType}",
                    NameCollisionOption.GenerateUniqueName);
            }
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
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