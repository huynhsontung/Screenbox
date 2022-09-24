#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Screenbox.ViewModels;

namespace Screenbox.Services
{
    internal sealed class SystemMediaTransportControlsService : ISystemMediaTransportControlsService
    {
        public SystemMediaTransportControls TransportControls { get; }

        private DateTime _lastUpdated;

        public SystemMediaTransportControlsService()
        {
            TransportControls = SystemMediaTransportControls.GetForCurrentView();
            TransportControls.IsEnabled = true;
            TransportControls.IsPlayEnabled = true;
            TransportControls.IsPauseEnabled = true;
            TransportControls.IsStopEnabled = true;
            TransportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;
            TransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            TransportControls.DisplayUpdater.ClearAll();

            _lastUpdated = DateTime.MinValue;
        }

        public async Task UpdateTransportControlsDisplay(MediaViewModel? item)
        {
            SystemMediaTransportControlsDisplayUpdater displayUpdater = TransportControls.DisplayUpdater;
            displayUpdater.ClearAll();
            displayUpdater.AppMediaId = ReswPlusLib.Macros.ApplicationName;
            if (item == null)
            {
                return;
            }

            if (item.Source is StorageFile file)
            {
                if (file.ContentType.StartsWith("audio"))
                {
                    await displayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                    if (string.IsNullOrEmpty(displayUpdater.MusicProperties.Title))
                    {
                        displayUpdater.MusicProperties.Title = item.Name;
                    }
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

        public void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime)
        {
            if (DateTime.Now - _lastUpdated < TimeSpan.FromSeconds(5)) return;
            _lastUpdated = DateTime.Now;
            SystemMediaTransportControlsTimelineProperties timelineProps = new()
            {
                StartTime = startTime,
                MinSeekTime = startTime,
                Position = position,
                MaxSeekTime = endTime,
                EndTime = endTime
            };

            TransportControls.UpdateTimelineProperties(timelineProps);
        }

        public void UpdatePlaybackStatus(MediaPlaybackState state)
        {
            switch (state)
            {
                case MediaPlaybackState.None:
                    TransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaPlaybackState.Opening:
                    TransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Buffering:
                    TransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                    break;
                case MediaPlaybackState.Playing:
                    TransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case MediaPlaybackState.Paused:
                    TransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                default:
                    break;
            }
        }

        public void ClosePlayback()
        {
            TransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            TransportControls.DisplayUpdater.ClearAll();
        }
    }
}
