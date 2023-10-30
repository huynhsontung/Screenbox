﻿using System.Collections.Immutable;
using Windows.Storage;

namespace Screenbox.Core.Helpers;
public static class FilesHelpers
{
    public static ImmutableArray<string> SupportedAudioFormats { get; } =
        ImmutableArray.Create(".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", ".ogg", ".oga", ".opus", ".weba", ".flac", ".m4a");

    public static ImmutableArray<string> SupportedVideoFormats { get; } =
        ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm");

    public static ImmutableArray<string> SupportedPlaylistFormats { get; } =
        ImmutableArray.Create(".m3u8", ".m3u", ".ts");

    public static ImmutableArray<string> SupportedFormats { get; } =
        SupportedVideoFormats.AddRange(SupportedAudioFormats).AddRange(SupportedPlaylistFormats);

    public static bool IsSupportedAudio(this IStorageFile file) => SupportedAudioFormats.Contains(file.FileType.ToLowerInvariant());
    public static bool IsSupportedVideo(this IStorageFile file) => SupportedVideoFormats.Contains(file.FileType.ToLowerInvariant());
    public static bool IsSupportedPlaylist(this IStorageFile file) => SupportedPlaylistFormats.Contains(file.FileType.ToLowerInvariant());
    public static bool IsSupported(this IStorageFile file) => SupportedFormats.Contains(file.FileType.ToLowerInvariant());
}
