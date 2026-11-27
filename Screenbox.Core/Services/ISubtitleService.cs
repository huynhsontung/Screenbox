using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Playback;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

/// <summary>
/// Discovers external subtitle files and resolves preferred subtitle tracks.
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// Finds external subtitle files matching <paramref name="sourceFile"/> in the same directory.
    /// </summary>
    /// <param name="sourceFile">The media file to find matching subtitles for.</param>
    /// <param name="neighboringFilesQuery">An optional pre-existing neighboring files query to search first.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of matching external subtitle files.</returns>
    Task<IReadOnlyList<StorageFile>> FindSubtitlesForFileAsync(
        StorageFile sourceFile,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects the preferred subtitle track index from <paramref name="subtitleTracks"/> based on
    /// a comma-separated list of preferred languages or language tags.
    /// </summary>
    /// <param name="subtitleTracks">The available subtitle tracks.</param>
    /// <param name="preferredLanguages">A comma-separated string of preferred language tags or names.</param>
    /// <returns>The zero-based index of the preferred subtitle track, or <c>-1</c> if none matched.</returns>
    int FindPreferredSubtitleIndex(IReadOnlyList<SubtitleTrack> subtitleTracks, string preferredLanguages);
}
