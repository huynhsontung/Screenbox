#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Services;

/// <summary>
/// Stateless service for playlist operations
/// </summary>
public sealed class PlaylistService : IPlaylistService
{
    private readonly IMediaListFactory _mediaListFactory;

    public PlaylistService(IMediaListFactory mediaListFactory)
    {
        _mediaListFactory = mediaListFactory;
    }

    public async Task<Playlist> AddNeighboringFilesAsync(Playlist playlist, StorageFileQueryResult neighboringFilesQuery, StorageFile currentFile, CancellationToken cancellationToken = default)
    {
        try
        {
            var neighboringFiles = await neighboringFilesQuery.GetFilesAsync();
            var result = await _mediaListFactory.TryParseMediaListAsync(neighboringFiles, currentFile, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (result?.Items.Count > 0)
                return new Playlist(result.NextItem, result.Items, playlist);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception)
        {
            // Handle other errors
        }

        return playlist;
    }

    public Playlist ShufflePlaylist(Playlist playlist, int? preserveIndex = null)
    {
        var shuffled = playlist.Clone();
        var random = new Random();

        if (preserveIndex.HasValue && preserveIndex.Value >= 0 && preserveIndex.Value < shuffled.Items.Count)
        {
            var activeItem = shuffled.Items[preserveIndex.Value];
            shuffled.Items.RemoveAt(preserveIndex.Value);
            Shuffle(shuffled.Items, random);
            shuffled.Items.Insert(0, activeItem);
            shuffled.CurrentIndex = 0;
        }
        else
        {
            Shuffle(shuffled.Items, random);
        }

        return shuffled;
    }

    public Playlist RestoreFromShuffle(Playlist playlist)
    {
        Guard.IsNotNull(playlist.ShuffleBackup, nameof(playlist.ShuffleBackup));
        var shuffleBackup = playlist.ShuffleBackup;
        var restored = new Playlist();
        var backup = new List<MediaViewModel>(shuffleBackup.OriginalPlaylist);

        foreach (var removal in shuffleBackup.Removals)
        {
            backup.Remove(removal);
        }

        foreach (var media in backup)
        {
            restored.Items.Add(media);
        }

        // Try to maintain current item
        if (playlist.CurrentItem != null)
        {
            restored.CurrentIndex = restored.Items.IndexOf(playlist.CurrentItem);
        }

        restored.ShuffleMode = false;
        restored.ShuffleBackup = null;
        restored.NeighboringFilesQuery = playlist.NeighboringFilesQuery;
        restored.LastUpdated = playlist.LastUpdated;

        return restored;
    }

    public int? GetNextIndex(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, StorageFileQueryResult? neighboringFilesQuery = null)
    {
        if (playlistCount == 0) return null;

        if (playlistCount == 1)
        {
            return neighboringFilesQuery != null ? null : currentIndex; // Signal neighboring file check needed
        }

        if (repeatMode == MediaPlaybackAutoRepeatMode.List)
        {
            return currentIndex == playlistCount - 1 ? 0 : currentIndex + 1;
        }

        return currentIndex < playlistCount - 1 ? currentIndex + 1 : null;
    }

    public int? GetPreviousIndex(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode)
    {
        if (playlistCount == 0) return null;

        if (repeatMode == MediaPlaybackAutoRepeatMode.List)
        {
            return currentIndex == 0 ? playlistCount - 1 : currentIndex - 1;
        }

        return currentIndex > 0 ? currentIndex - 1 : null;
    }

    public IReadOnlyList<int> GetMediaBufferIndices(int currentIndex, int playlistCount, MediaPlaybackAutoRepeatMode repeatMode, int bufferSize = 5)
    {
        if (currentIndex < 0 || playlistCount == 0) return Array.Empty<int>();

        int startIndex = Math.Max(currentIndex - 2, 0);
        int endIndex = Math.Min(currentIndex + 2, playlistCount - 1);
        var indices = new List<int>();

        for (int i = startIndex; i <= endIndex; i++)
        {
            indices.Add(i);
        }

        // Add wrap-around indices for list repeat mode
        if (repeatMode == MediaPlaybackAutoRepeatMode.List && indices.Count < bufferSize)
        {
            if (startIndex == 0 && endIndex < playlistCount - 1)
            {
                indices.Add(playlistCount - 1);
            }

            if (startIndex > 0 && endIndex == playlistCount - 1)
            {
                indices.Insert(0, 0);
            }
        }

        return indices.AsReadOnly();
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
