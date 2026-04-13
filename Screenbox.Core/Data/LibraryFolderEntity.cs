using Screenbox.Core.Enums;

namespace Screenbox.Core.Data;

/// <summary>
/// Stores the folder paths that belong to a storage library.
/// Used to detect whether the library definition has changed since the last cache.
/// </summary>
internal class LibraryFolderEntity
{
    public int Id { get; set; }

    /// <summary>Whether this folder belongs to the Music or Video library.</summary>
    public MediaPlaybackType LibraryType { get; set; }

    public string Path { get; set; } = string.Empty;
}
