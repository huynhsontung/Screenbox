namespace Screenbox.Core.Enums;

//[System.Obsolete("Remove the attribute once the transition from Protobuf to Microsoft.Data.Sqlite has been in place for some time.")]
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
