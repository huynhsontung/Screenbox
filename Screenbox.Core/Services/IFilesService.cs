using System.Collections.Generic;
using System.Threading.Tasks;
using Screenbox.Core.Playback;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

public interface IFilesService
{
    public Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null);
    public Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile,
        StorageFileQueryResult neighboringFilesQuery);
    public Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
        StorageFileQueryResult neighboringFilesQuery);
    public StorageItemQueryResult GetSupportedItems(StorageFolder folder);
    public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder);
    public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
    public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats);
    public IAsyncOperation<StorageFile> PickSaveFileAsync(string suggestedFileName, IDictionary<string, IList<string>> fileTypes, PickerLocationId startLocation = PickerLocationId.ComputerFolder);
    public IAsyncOperation<StorageFolder> PickFolderAsync();
    public Task OpenFileLocationAsync(string path);
    public Task OpenFileLocationAsync(StorageFile file);
    public Task OpenFolderAsync(StorageFolder folder);
    public string? AddToRecent(IStorageItem item);

    /// <summary>
    /// Retrieves the frame captures <see cref="StorageFolder"/> for the given token.
    /// </summary>
    /// <param name="token">The token of the frame captures folder.</param>
    /// <returns>
    /// When this method completes successfully, it returns the frame captures folder
    /// that is associated with the specified token.
    /// </returns>
    IAsyncOperation<StorageFolder> GetFrameCaptureFolderAsync(string token);

    /// <summary>
    /// Persists <paramref name="folder"/> in the application's future-access list for frame captures
    /// and removes <paramref name="previousToken"/> if present.
    /// </summary>
    /// <param name="folder">The selected frame capture folder.</param>
    /// <param name="previousToken">An optional previous access token to release.</param>
    /// <returns>The newly registered future-access list token.</returns>
    string SetFrameCaptureFolder(StorageFolder folder, string? previousToken = null);

    /// <summary>
    /// Captures a snapshot of the current video frame from <paramref name="mediaPlayer"/> and saves it
    /// to the frame capture folder associated with <paramref name="frameCaptureFolderToken"/>.
    /// </summary>
    /// <param name="mediaPlayer">The active media player to capture a frame from.</param>
    /// <param name="frameCaptureFolderToken">The configured frame capture folder token.</param>
    /// <returns>The saved snapshot <see cref="StorageFile"/>.</returns>
    Task<StorageFile> SaveSnapshotAsync(IMediaPlayer mediaPlayer, string frameCaptureFolderToken);
}
