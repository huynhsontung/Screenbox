using Windows.Storage;

namespace Screenbox.Core.MediaSources;

/// <summary>
/// Represents a media source backed by a local or network file.
/// Can be backed by an eager <see cref="Windows.Storage.StorageFile"/> or a lazily resolved file path.
/// </summary>
public interface IFileMediaSource : IMediaSource
{
    /// <summary>
    /// Gets the absolute path of the file on disk or network.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the file extension, including the leading period (e.g., ".mp4").
    /// </summary>
    string FileType { get; }

    /// <summary>
    /// Gets or sets the access token (e.g. from MRU or FutureAccessList) used to restore access to this file across app sessions.
    /// </summary>
    string? Token { get; set; }

    /// <summary>
    /// Gets the cached <see cref="Windows.Storage.StorageFile"/> if already resolved or provided upfront; otherwise <see langword="null"/>.
    /// </summary>
    StorageFile? StorageFile { get; }

    /// <summary>
    /// Updates the cached <see cref="Windows.Storage.StorageFile"/> instance.
    /// </summary>
    void UpdateStorageFile(StorageFile file);
}
