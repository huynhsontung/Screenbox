using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Helpers;
using Screenbox.Core.Playback;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for external subtitle file discovery and subtitle track language matching.
/// </summary>
public sealed class SubtitleService : ISubtitleService
{
    private static readonly char[] TokenSeparators =
        [' ', '.', '_', '-', '[', ']', '(', ')', '{', '}', ',', ';', '"', '\''];

    private readonly IFilesService _filesService;
    private readonly ILogger<SubtitleService> _logger;

    public SubtitleService(IFilesService filesService, ILogger<SubtitleService> logger)
    {
        _filesService = filesService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<StorageFile>> FindSubtitlesForFileAsync(
        StorageFile sourceFile,
        StorageFileQueryResult? neighboringFilesQuery = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);

        if (cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<StorageFile>();
        }

        string[] tokens = TokenizeFileName(sourceFile.Name);
        if (tokens.Length == 0)
        {
            return Array.Empty<StorageFile>();
        }

        if (neighboringFilesQuery is not null)
        {
            try
            {
                IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(0, 50);
                if (cancellationToken.IsCancellationRequested)
                {
                    return Array.Empty<StorageFile>();
                }

                return MatchSubtitleCandidates(sourceFile.Name, files, f => f.Name, f => f.IsSupportedSubtitle());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to enumerate matching subtitle files for file type '{FileType}'.", sourceFile.FileType);
                return Array.Empty<StorageFile>();
            }
        }

        // Fallback to creating a new query with subtitle filter
        // STRATEGY A: Strict "Skeleton" Match ("Iron.Man.2008" -> "Iron*Man*2008*")
        string strictPattern = string.Join("*", tokens) + "*";

        QueryOptions options = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedSubtitleFormats)
        {
            ApplicationSearchFilter = $"System.FileName:~\"{strictPattern}\""
        };

        StorageFileQueryResult? query = await _filesService.GetNeighboringFilesQueryAsync(sourceFile, options);
        if (query is null || cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<StorageFile>();
        }

        IReadOnlyList<StorageFile> subtitles = await query.GetFilesAsync(0, 50);

        // STRATEGY B: Fallback (Partial Tokens Match)
        // If "Iron*Man*2008*" fails, try "Iron*Man*"
        if (subtitles.Count == 0 && tokens.Length > 1 && !cancellationToken.IsCancellationRequested)
        {
            string fallbackPattern = string.Join("*", tokens.Take(Math.Min(tokens.Length - 1, 3))) + "*";
            options.ApplicationSearchFilter = $"System.FileName:~\"{fallbackPattern}\"";
            query.ApplyNewQueryOptions(options);
            subtitles = await query.GetFilesAsync(0, 50);
        }

        return subtitles;
    }

    /// <inheritdoc/>
    public int FindPreferredSubtitleIndex(IReadOnlyList<SubtitleTrack> subtitleTracks, string preferredLanguages)
    {
        ArgumentNullException.ThrowIfNull(subtitleTracks);

        if (subtitleTracks.Count == 0 || string.IsNullOrEmpty(preferredLanguages))
        {
            return -1;
        }

        // If there is only one subtitle and user has a persistent language preference, select it
        if (subtitleTracks.Count == 1)
        {
            return 0;
        }

        string[] langPreferences = preferredLanguages.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (string language in langPreferences)
        {
            for (int i = 0; i < subtitleTracks.Count; i++)
            {
                SubtitleTrack subtitleTrack = subtitleTracks[i];
                // Try to match language tag first, then language name
                if (language == subtitleTrack.LanguageTag ||
                    language.Equals(subtitleTrack.Language, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Filters candidate file names to supported subtitle files matching <paramref name="sourceFileName"/>
    /// using a strict token skeleton match followed by a partial-token fallback match.
    /// </summary>
    /// <param name="sourceFileName">The video file name (with or without extension).</param>
    /// <param name="candidateFileNames">Candidate file names from the same directory.</param>
    /// <returns>The matching subtitle file names.</returns>
    public static IReadOnlyList<string> MatchSubtitleFileNames(
        string sourceFileName,
        IReadOnlyList<string> candidateFileNames)
    {
        ArgumentNullException.ThrowIfNull(candidateFileNames);

        return MatchSubtitleCandidates(
            sourceFileName,
            candidateFileNames,
            static name => name,
            static name => FilesHelpers.IsSupportedSubtitle(Path.GetExtension(name)));
    }

    private static IReadOnlyList<T> MatchSubtitleCandidates<T>(
        string sourceFileName,
        IReadOnlyList<T> candidates,
        Func<T, string> nameSelector,
        Func<T, bool> isSubtitlePredicate)
    {
        string[] tokens = TokenizeFileName(sourceFileName);
        if (tokens.Length == 0 || candidates.Count == 0)
        {
            return Array.Empty<T>();
        }

        List<string> escapedTokens = tokens.Select(Regex.Escape).ToList();

        // STRATEGY A: Strict "Skeleton" Match
        string strictRegexPattern = "^" + string.Join(".*", escapedTokens) + ".*$";
        T[] matches = candidates
            .Where(c => isSubtitlePredicate(c) && Regex.IsMatch(nameSelector(c), strictRegexPattern, RegexOptions.IgnoreCase))
            .ToArray();

        if (matches.Length == 0 && tokens.Length > 1)
        {
            // STRATEGY B: Fallback (Partial Tokens Match)
            string fallbackPattern = "^" + string.Join(".*", escapedTokens.Take(Math.Min(escapedTokens.Count - 1, 3))) + ".*$";
            matches = candidates
                .Where(c => isSubtitlePredicate(c) && Regex.IsMatch(nameSelector(c), fallbackPattern, RegexOptions.IgnoreCase))
                .ToArray();
        }

        return matches;
    }

    private static string[] TokenizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Array.Empty<string>();
        }

        string rawName = Path.GetFileNameWithoutExtension(fileName);
        return rawName.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
    }
}
