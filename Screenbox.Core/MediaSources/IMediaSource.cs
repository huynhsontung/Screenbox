using Screenbox.Core.Enums;

namespace Screenbox.Core.MediaSources;

/// <summary>
/// Represents the identity and intrinsic capabilities of a media source.
/// </summary>
public interface IMediaSource
{
    /// <summary>
    /// The canonical location or identifier of the media source (e.g., file path or URI string).
    /// </summary>
    string Location { get; }

    /// <summary>
    /// The display name of the media source (e.g., file name or URI segment).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The inferred media playback type (e.g., Video, Music, Playlist, Unknown).
    /// </summary>
    MediaPlaybackType MediaType { get; }
}
