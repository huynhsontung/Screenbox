#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for library management operations.
/// Does not access or modify <see cref="Contexts.LibraryContext"/> directly;
/// all context mutations are the responsibility of the caller (typically <see cref="Coordinators.ILibraryCoordinator"/>).
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Initialize the music library
    /// </summary>
    Task<StorageLibrary> InitializeMusicLibraryAsync();

    /// <summary>
    /// Initialize the videos library
    /// </summary>
    Task<StorageLibrary> InitializeVideosLibraryAsync();

    /// <summary>
    /// Fetch music from the library. Returns a <see cref="MusicLibrary"/> that the caller
    /// should apply to the library context. Intermediate results are reported via <paramref name="progress"/>.
    /// </summary>
    Task<MusicLibrary> FetchMusicAsync(
        StorageLibrary library,
        StorageFileQueryResult queryResult,
        bool useCache,
        CancellationToken cancellationToken,
        IProgress<MusicLibrary>? progress = null);

    /// <summary>
    /// Fetch videos from the library. Returns a <see cref="VideosLibrary"/> that the caller
    /// should apply to the library context. Intermediate results are reported via <paramref name="progress"/>.
    /// </summary>
    Task<VideosLibrary> FetchVideosAsync(
        StorageLibrary library,
        StorageFileQueryResult queryResult,
        bool useCache,
        CancellationToken cancellationToken,
        IProgress<VideosLibrary>? progress = null);

    /// <summary>
    /// Creates a query for the user's music library.
    /// </summary>
    StorageFileQueryResult CreateMusicLibraryQuery(bool useIndexer);

    /// <summary>
    /// Creates a query for the user's videos library.
    /// </summary>
    StorageFileQueryResult CreateVideosLibraryQuery(bool useIndexer);
}

