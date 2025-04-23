﻿#nullable enable

using Screenbox.Core.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Services
{
    public sealed class SystemMediaTransportControlsService : ISystemMediaTransportControlsService
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

        public async Task UpdateTransportControlsDisplayAsync(MediaViewModel? item)
        {
            SystemMediaTransportControlsDisplayUpdater displayUpdater = TransportControls.DisplayUpdater;
            displayUpdater.ClearAll();
            displayUpdater.AppMediaId = Package.Current.DisplayName;
            if (item == null)
            {
                return;
            }

            try
            {
                if (item.Source is StorageFile file)
                {
                    if (file.IsSupportedAudio())
                    {
                        bool success = await displayUpdater.CopyFromFileAsync(MediaPlaybackType.Music, file);
                        if (success && string.IsNullOrEmpty(displayUpdater.MusicProperties.Title))
                        {
                            displayUpdater.MusicProperties.Title = item.Name;
                        }
                    }
                    else if (file.IsSupportedVideo())
                    {
                        bool success = await displayUpdater.CopyFromFileAsync(MediaPlaybackType.Video, file);
                        if (success && string.IsNullOrEmpty(displayUpdater.VideoProperties.Title))
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
            catch (Exception)
            {
                // System.Exception: The request is not supported. The media type has not been initialized. Please provide a valid media type first in order to access these properties.
                // Pass
            }
        }

        public void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime, TimeSpan updateInterval = default)
        {
            if (updateInterval < TimeSpan.FromSeconds(1)) updateInterval = TimeSpan.FromSeconds(5);
            if (DateTime.Now - _lastUpdated < updateInterval) return;
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
