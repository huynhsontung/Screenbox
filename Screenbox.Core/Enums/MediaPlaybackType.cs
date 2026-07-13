namespace Screenbox.Core.Enums;

// TODO: Remove the attribute in version 1.0, after giving older versions enough time to migrate away from Protobuf.
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum MediaPlaybackType
{
    /// <summary>The media type is unknown.</summary>
    Unknown,
    /// <summary>The media type is audio music.</summary>
    Music,
    /// <summary>The media type is video.</summary>
    Video,
    /// <summary>The media type is an image.</summary>
    Image,
    /// <summary>The media type is a playlist.</summary>
    Playlist,
}
