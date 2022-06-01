#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.System;
using LibVLCSharp.Shared;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal class SystemMediaTransportControlsViewModel : ObservableObject
    {
        private MediaPlayer? VlcPlayer => _mediaPlayerService.VlcPlayer;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly SystemMediaTransportControls _transportControls;
        private readonly IMediaPlayerService _mediaPlayerService;
        private readonly PlaylistViewModel _playlistViewModel;
        private DateTime _lastUpdated;

        public SystemMediaTransportControlsViewModel(
            IMediaPlayerService mediaPlayerService, PlaylistViewModel playlistViewModel)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _playlistViewModel = playlistViewModel;
            _playlistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
            _playlistViewModel.NextCommand.CanExecuteChanged += NextCommandOnCanExecuteChanged;
            _playlistViewModel.PreviousCommand.CanExecuteChanged += PreviousCommandOnCanExecuteChanged;
            _mediaPlayerService = mediaPlayerService;
            _mediaPlayerService.PlayerInitialized += OnPlayerInitialized;
            _mediaPlayerService.TimeChanged += OnTimeChanged;

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

        private void OnPlayerInitialized(object sender, EventArgs e)
        {
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            RegisterPlaybackEvents(VlcPlayer);
        }

        private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            _mediaPlayerService.SetTime(args.RequestedPlaybackPosition.TotalMilliseconds);
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (DateTime.Now - _lastUpdated < TimeSpan.FromSeconds(5)) return;
            _lastUpdated = DateTime.Now;
            Guard.IsNotNull(VlcPlayer, nameof(VlcPlayer));
            SystemMediaTransportControlsTimelineProperties timelineProps = new()
            {
                StartTime = TimeSpan.Zero,
                MinSeekTime = TimeSpan.Zero,
                Position = TimeSpan.FromMilliseconds(e.Time),
                MaxSeekTime = TimeSpan.FromMilliseconds(VlcPlayer.Length),
                EndTime = TimeSpan.FromMilliseconds(VlcPlayer.Length)
            };

            _transportControls.UpdateTimelineProperties(timelineProps);
        }

        private void RegisterPlaybackEvents(MediaPlayer vlcPlayer)
        {
            vlcPlayer.Paused += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
            vlcPlayer.EndReached += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
            vlcPlayer.Stopped += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
            vlcPlayer.Playing += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
            vlcPlayer.EncounteredError += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            vlcPlayer.Opening += (_, _) => _transportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
        }

        private void TransportControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    _mediaPlayerService.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    _mediaPlayerService.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    _mediaPlayerService.Stop();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    _playlistViewModel.PreviousCommand.Execute(null);
                    break;
                case SystemMediaTransportControlsButton.Next:
                    _playlistViewModel.NextCommand.Execute(null);
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    _mediaPlayerService.Seek(30000);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    _mediaPlayerService.Seek(-30000);
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
