#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.ViewModels;
using Windows.Storage;

namespace Screenbox.Core.Services
{
    public interface IMediaParsingService
    {
        /// <summary>
        /// Create a playlist from storage items
        /// </summary>
        Task<PlaylistCreateResult?> CreatePlaylistAsync(IReadOnlyList<IStorageItem> storageItems, StorageFile? playNext = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a playlist from a single media item
        /// </summary>
        Task<PlaylistCreateResult> CreatePlaylistAsync(MediaViewModel media, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a playlist from a storage file
        /// </summary>
        Task<PlaylistCreateResult> CreatePlaylistAsync(StorageFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a playlist from a URI
        /// </summary>
        Task<PlaylistCreateResult> CreatePlaylistAsync(Uri uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parse sub-media items recursively (for playlist files)
        /// </summary>
        Task<List<MediaViewModel>> ParseSubMediaRecursiveAsync(MediaViewModel source, CancellationToken cancellationToken = default);
    }

    public sealed class PlaylistCreateResult
    {
        public MediaViewModel PlayNext { get; }
        public List<MediaViewModel> Playlist { get; }

        public PlaylistCreateResult(MediaViewModel playNext, List<MediaViewModel> playlist)
        {
            PlayNext = playNext;
            Playlist = playlist;
        }

        public PlaylistCreateResult(MediaViewModel playNext) : this(playNext, new List<MediaViewModel> { playNext }) { }
    }
}
