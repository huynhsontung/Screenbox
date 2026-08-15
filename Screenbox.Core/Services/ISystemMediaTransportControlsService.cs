using System;
using System.Threading.Tasks;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Media.Playback;

namespace Screenbox.Core.Services;

public interface ISystemMediaTransportControlsService
{
    SystemMediaTransportControls TransportControls { get; }
    Task UpdateTransportControlsDisplayAsync(MediaViewModel? item);
    void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime);
    void UpdatePlaybackStatus(MediaPlaybackState state);
    void ClosePlayback();
}
