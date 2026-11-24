using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Screenbox.Core.Helpers;

public static class FilesHelpers
{
    // Backed by raw string[] (not ImmutableArray<string>) so these collections can cross the
    // WinRT ABI boundary (e.g. QueryOptions) directly without CsWinRT marshaling failures.
    public static IReadOnlyList<string> SupportedAudioFormats { get; } =
        new[] { ".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", ".ogg", ".oga", ".opus", ".weba", ".flac", ".m4a", ".m4b", ".wv", ".wvc", ".aiff", ".aif", ".aifc", ".ac3", ".ape", ".dts", ".nist", ".ra", ".spx" };

    public static IReadOnlyList<string> SupportedVideoFormats { get; } =
        new[] { ".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm", ".rm", ".rmvb", ".asf", ".wm", ".wtv", ".f4v", ".swf", ".vob", ".mxf", ".ogv", ".ogm", ".ts", ".mts", ".m2ts", ".m2t" };

    public static IReadOnlyList<string> SupportedPlaylistFormats { get; } =
        new[] { ".m3u8", ".m3u" };

    public static IReadOnlyList<string> SupportedFormats { get; } =
        SupportedVideoFormats.Concat(SupportedAudioFormats).Concat(SupportedPlaylistFormats).ToArray();

    public static IReadOnlyList<string> SupportedSubtitleFormats { get; } =
        new[] { ".srt", ".vtt", ".ass", ".idx", ".sub" };

    public static bool IsSupportedAudio(this IStorageFile file) => IsSupportedAudio(file.FileType);
    public static bool IsSupportedVideo(this IStorageFile file) => IsSupportedVideo(file.FileType);
    public static bool IsSupportedPlaylist(this IStorageFile file) => IsSupportedPlaylist(file.FileType);
    public static bool IsSupported(this IStorageFile file) => IsSupported(file.FileType);
    public static bool IsSupportedSubtitle(this IStorageFile file) => IsSupportedSubtitle(file.FileType);

    public static bool IsSupportedAudio(string extension) => SupportedAudioFormats.Contains(NormalizeExtension(extension));
    public static bool IsSupportedVideo(string extension) => SupportedVideoFormats.Contains(NormalizeExtension(extension));
    public static bool IsSupportedPlaylist(string extension) => SupportedPlaylistFormats.Contains(NormalizeExtension(extension));
    public static bool IsSupported(string extension) => SupportedFormats.Contains(NormalizeExtension(extension));
    public static bool IsSupportedSubtitle(string extension) => SupportedSubtitleFormats.Contains(NormalizeExtension(extension));

    public static MediaPlaybackType GetMediaTypeForExtension(string extension)
    {
        if (IsSupportedVideo(extension)) return MediaPlaybackType.Video;
        if (IsSupportedAudio(extension)) return MediaPlaybackType.Music;
        if (IsSupportedPlaylist(extension)) return MediaPlaybackType.Playlist;
        return MediaPlaybackType.Unknown;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return string.Empty;
        return extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
    }

    public static MediaPlaybackType GetMediaTypeForFile(IStorageFile file)
    {
        if (file.IsSupportedVideo()) return MediaPlaybackType.Video;
        if (file.IsSupportedAudio()) return MediaPlaybackType.Music;
        if (file.ContentType.StartsWith("image")) return MediaPlaybackType.Image;
        if (file.IsSupportedPlaylist()) return MediaPlaybackType.Playlist;
        return MediaPlaybackType.Unknown;
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageFile"/> from a file-system path.
    /// Returns <see langword="null"/> when the file is inaccessible or does not exist.
    /// </summary>
    public static async Task<StorageFile?> TryGetFileFromPathAsync(string path)
    {
        try
        {
            return await StorageFile.GetFileFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageFolder"/> from a file-system path.
    /// Returns <see langword="null"/> when the folder is inaccessible or does not exist.
    /// </summary>
    public static async Task<StorageFolder?> TryGetFolderFromPathAsync(string path)
    {
        try
        {
            return await StorageFolder.GetFolderFromPathAsync(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageFile"/> from the application's access cache
    /// (<see cref="StorageApplicationPermissions.MostRecentlyUsedList"/> or
    /// <see cref="StorageApplicationPermissions.FutureAccessList"/>) using the specified token.
    /// </summary>
    public static async Task<StorageFile?> TryGetFileFromTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
        }
        catch (Exception)
        {
            // Access cache lookup failed or unavailable.
        }

        return null;
    }

    /// <summary>
    /// Attempts to retrieve a <see cref="StorageFolder"/> from the application's access cache
    /// (<see cref="StorageApplicationPermissions.MostRecentlyUsedList"/> or
    /// <see cref="StorageApplicationPermissions.FutureAccessList"/>) using the specified token.
    /// </summary>
    public static async Task<StorageFolder?> TryGetFolderFromTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            if (StorageApplicationPermissions.MostRecentlyUsedList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(
                    token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
        }
        catch (Exception)
        {
            // Access cache lookup failed or unavailable.
        }

        return null;
    }
}
