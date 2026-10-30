namespace Screenbox.Core;

/// <summary>
/// Defines members that identify a playback item and indicate whether it is playing.
/// </summary>
public interface IPlaybable
{
    /// <summary>
    /// Gets the name of the playback item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value that indicates whether the playback item is currently playing.
    /// </summary>
    /// <value><see langword="true"/> if the playback item is playing; otherwise, <see langword="false"/>.</value>
    bool IsPlaying { get; }
}
