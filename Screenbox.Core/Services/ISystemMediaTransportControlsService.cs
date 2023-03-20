#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Screenbox.ViewModels;

namespace Screenbox.Core.Services
{
    public interface ISystemMediaTransportControlsService
    {
        SystemMediaTransportControls TransportControls { get; }
        Task UpdateTransportControlsDisplay(MediaViewModel? item);
        void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime);
        void UpdatePlaybackStatus(MediaPlaybackState state);
        void ClosePlayback();
    }
}