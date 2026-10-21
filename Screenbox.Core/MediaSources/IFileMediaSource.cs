using System.Threading.Tasks;
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
    /// Gets the cached <see cref="Windows.Storage.StorageFile"/> if already resolved or provided upfront; otherwise <see langword="null"/>.
    /// </summary>
    StorageFile? StorageFile { get; }

    /// <summary>
    /// Returns the cached <see cref="Windows.Storage.StorageFile"/>, or asynchronously resolves and caches it from <see cref="Path"/>.
    /// </summary>
    Task<StorageFile?> GetStorageFileAsync();

    /// <summary>
    /// Updates the cached <see cref="Windows.Storage.StorageFile"/> instance.
    /// </summary>
    void UpdateStorageFile(StorageFile file);
}
