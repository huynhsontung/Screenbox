namespace Screenbox.Core.Data;

/// <summary>
/// Stores the last known playback position for a media file.
/// Replaces the legacy <c>MediaLastPosition</c> Protobuf model.
/// </summary>
internal class PlaybackProgressEntity
{
    public int Id { get; set; }

    /// <summary>Unique file path or URI that identifies the media item.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Stored position as <see cref="System.TimeSpan.Ticks"/>.</summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// LRU ordering index — lower value means more recently accessed.
    /// </summary>
    public int SortOrder { get; set; }
}
