using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents a media metadata entry with a key and its associated value.
/// </summary>
public sealed record MediaMetadata
{
    /// <summary>
    /// Gets the key that identifies the media metadata entry.
    /// </summary>
    /// <value>A value indicating the metadata entry.</value>
    public Property Key { get; }

    /// <summary>
    /// Gets the value of the media metadata entry.
    /// </summary>
    /// <value>The string value of the metadata entry.</value>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaMetadata"/> record
    /// using the specified key and value.
    /// </summary>
    /// <param name="key">The key that identifies the metadata entry.</param>
    /// <param name="value">The value associated with the metadata entry.</param>
    public MediaMetadata(Property key, string value)
    {
        Key = key;
        Value = value;
    }
}
