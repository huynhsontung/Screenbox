#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Screenbox.Services;
using Screenbox.Core.Playback;

namespace Screenbox.ViewModels
{
    internal class SystemMediaTransportControlsViewModel : ObservableObject
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SystemMediaTransportControls _transportControls;
        private readonly PlaylistViewModel _playlistViewModel;
        private IMediaPlayer? _mediaPlayer;
        private DateTime _lastUpdated;

        public SystemMediaTransportControlsViewModel(
            LibVlcService libVlcService,
            PlaylistViewModel playlistViewModel)
        {
            libVlcService.Initialized += LibVlcService_Initialized;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _playlistViewModel = playlistViewModel;
            _playlistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
            _playlistViewModel.NextCommand.CanExecuteChanged += NextCommandOnCanExecuteChanged;
            _playlistViewModel.PreviousCommand.CanExecuteChanged += PreviousCommandOnCanExecuteChanged;

            _transportControls = SystemMediaTransportControls.GetForCurrentView();
            _transportControls.ButtonPressed += TransportControlsButtonPressed;
            _transportControls.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;
            _transportControls.AutoRepeatModeChangeRequested += TransportControlsOnAutoRepeatModeChangeRequested;
            _transportControls.IsEnabled = true;
            _transportControls.IsPlayEnabled = true;
            _transportControls.IsPauseEnabled = true;
            _transportControls.IsStopEnabled = true;
            _transportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;
            _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            SystemMediaTransportControlsDisplayUpdater displayUpdater = _transportControls.DisplayUpdater;
            displayUpdater.AppMediaId = "Screenbox";
            displayUpdater.Update();

            _lastUpdated = DateTime.MinValue;
        }

        private void LibVlcService_Initialized(LibVlcService sender, Core.MediaPlayerInitializedEventArgs args)
        {
            _mediaPlayer = args.MediaPlayer;
            _mediaPlayer.PositionChanged += OnTimeChanged;
            RegisterPlaybackEvents(_mediaPlayer);
        }

        private void TransportControlsOnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() => _playlistViewModel.RepeatMode = Convert(args.RequestedAutoRepeatMode));
        }

        private void PreviousCommandOnCanExecuteChanged(object sender, EventArgs e)
        {
            _transportControls.IsPreviousEnabled = _playlistViewModel.PreviousCommand.CanExecute(null);
        }

        private void NextCommandOnCanExecuteChanged(object sender, EventArgs e)
        {
            _transportControls.IsNextEnabled = _playlistViewModel.NextCommand.CanExecute(null);
        }

        private async void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_playlistViewModel.PlayingItem) when _playlistViewModel.PlayingItem != null:
                    await UpdateTransportControlsDisplay(_playlistViewModel.PlayingItem);
                    break;
                case nameof(_playlistViewModel.RepeatMode):
                    _transportControls.AutoRepeatMode = Convert(_playlistViewModel.RepeatMode);
                    break;
            }
        }

        private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Position = args.RequestedPlaybackPosition;
        }

        private void OnTimeChanged(IMediaPlayer sender, object? args)
        {
            if (DateTime.Now - _lastUpdated < TimeSpan.FromSeconds(5)) return;
            _lastUpdated = DateTime.Now;
            SystemMediaTransportControlsTimelineProperties timelineProps = new()
            {
                StartTime = TimeSpan.Zero,
                MinSeekTime = TimeSpan.Zero,
                Position = sender.Position,
                MaxSeekTime = sender.NaturalDuration,
                EndTime = sender.NaturalDuration
            };

            _transportControls.UpdateTimelineProperties(timelineProps);
        }

        private void RegisterPlaybackEvents(IMediaPlayer player)
        {
            player.PlaybackStateChanged += (sender, _) =>
            {
                switch (sender.PlaybackState)
                {
                    case Windows.Media.Playback.MediaPlaybackState.None:
                        _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Opening:
                        _transportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Buffering:
                        _transportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Playing:
                        _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Paused:
                        _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;
                    default:
                        break;
                }
            };

            player.MediaEnded += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
            player.MediaFailed += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
        }

        private void TransportControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (_mediaPlayer == null) return;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    _mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    _mediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    _mediaPlayer.Source = null;
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    _playlistViewModel.PreviousCommand.Execute(null);
                    break;
                case SystemMediaTransportControlsButton.Next:
                    _playlistViewModel.NextCommand.Execute(null);
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    _mediaPlayer.Position += TimeSpan.FromSeconds(10);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    _mediaPlayer.Position -= TimeSpan.FromSeconds(10);
                    break;
            }
        }

        private async Task UpdateTransportControlsDisplay(MediaViewModel item)
        {
            SystemMediaTransportControlsDisplayUpdater displayUpdater = _transportControls.DisplayUpdater;
            if (item.Source is StorageFile file)
            {
                if (file.ContentType.StartsWith("audio"))
                {
                    await displayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                }
                else if (file.ContentType.StartsWith("video"))
                {
                    await displayUpdater.CopyFromFileAsync(MediaPlaybackType.Video, file);
                    if (string.IsNullOrEmpty(displayUpdater.VideoProperties.Title))
                    {
                        displayUpdater.VideoProperties.Title = item.Name;
                    }
                }
            }
            
            // DisplayUpdater can only have type of Video, Audio, or Image
            if (displayUpdater.Type == MediaPlaybackType.Unknown)
            {
                displayUpdater.Type = MediaPlaybackType.Video;
                displayUpdater.VideoProperties.Title = item.Name;
            }

            displayUpdater.Update();
        }

        private static RepeatMode Convert(MediaPlaybackAutoRepeatMode systemRepeatMode) => systemRepeatMode switch
        {
            MediaPlaybackAutoRepeatMode.None => RepeatMode.Off,
            MediaPlaybackAutoRepeatMode.Track => RepeatMode.One,
            MediaPlaybackAutoRepeatMode.List => RepeatMode.All,
            _ => RepeatMode.Off
        };

        private static MediaPlaybackAutoRepeatMode Convert(RepeatMode repeatMode) => repeatMode switch
        {
            RepeatMode.Off => MediaPlaybackAutoRepeatMode.None,
            RepeatMode.All => MediaPlaybackAutoRepeatMode.List,
            RepeatMode.One => MediaPlaybackAutoRepeatMode.Track,
            _ => MediaPlaybackAutoRepeatMode.None
        };
    }
}
