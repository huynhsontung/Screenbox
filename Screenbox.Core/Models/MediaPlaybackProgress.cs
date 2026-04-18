using System;

namespace Screenbox.Core.Models;

/// <summary>
/// In-memory record of the last known playback position for a media item.
/// </summary>
internal sealed class MediaPlaybackProgress
{
    public string Location { get; set; }

    public TimeSpan Position { get; set; }

    public MediaPlaybackProgress(string location, TimeSpan position)
    {
        Location = location;
        Position = position;
    }
}
