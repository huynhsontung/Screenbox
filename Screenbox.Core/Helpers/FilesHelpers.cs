using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Screenbox.Core.Enums;
using Windows.Storage;

namespace Screenbox.Core.Helpers;

public static class FilesHelpers
{
    public static ImmutableArray<string> SupportedAudioFormats { get; } =
        ImmutableArray.Create(".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", ".ogg", ".oga", ".opus", ".weba", ".flac", ".m4a", ".m4b", ".wv", ".wvc", ".aiff", ".aif", ".aifc", ".ac3", ".ape", ".dts", ".nist", ".ra", ".spx");

    public static ImmutableArray<string> SupportedVideoFormats { get; } =
        ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm", ".rm", ".rmvb", ".asf", ".wm", ".wtv", ".f4v", ".swf", ".vob", ".mxf", ".ogv", ".ogm", ".ts", ".mts", ".m2ts", ".m2t");

    public static ImmutableArray<string> SupportedPlaylistFormats { get; } =
        ImmutableArray.Create(".m3u8", ".m3u");

    public static ImmutableArray<string> SupportedFormats { get; } =
        SupportedVideoFormats.AddRange(SupportedAudioFormats).AddRange(SupportedPlaylistFormats);

    public static ImmutableArray<string> SupportedSubtitleFormats { get; } = ImmutableArray.Create(".srt", ".vtt", ".ass", ".idx", ".sub");

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
}
