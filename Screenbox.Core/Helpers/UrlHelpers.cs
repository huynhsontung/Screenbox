using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Screenbox.Core.Helpers;

public static class UrlHelpers
{
    private static readonly HashSet<string> UnsupportedUrlPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "youtu.be",
        "m.youtube.com", 
        "www.youtube.com",
        "vimeo.com",
        "www.vimeo.com",
        "twitch.tv",
        "www.twitch.tv",
        "dailymotion.com",
        "www.dailymotion.com",
        "facebook.com",
        "www.facebook.com",
        "instagram.com",
        "www.instagram.com",
        "tiktok.com",
        "www.tiktok.com"
    };

    private static readonly HashSet<string> SupportedMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".3g2",
        ".mp3", ".wav", ".wma", ".aac", ".ogg", ".flac", ".m4a", ".opus", 
        ".m3u8", ".m3u", ".ts", ".mts", ".m2ts", ".m2t"
    };

    /// <summary>
    /// Validates if a URL is supported for direct media playback
    /// </summary>
    /// <param name="uri">The URI to validate</param>
    /// <returns>True if the URL appears to be supported for direct playback, false otherwise</returns>
    public static bool IsSupportedMediaUrl(Uri uri)
    {
        if (uri == null || !uri.IsAbsoluteUri)
            return false;

        // Check if it's a local file URI - always supported if it passes other validations
        if (uri.IsFile && uri.IsLoopback)
            return true;

        // Check against known unsupported streaming platforms
        if (IsUnsupportedStreamingUrl(uri))
            return false;

        // Check if URL has a supported media file extension
        if (HasSupportedMediaExtension(uri))
            return true;

        // Allow HTTP/HTTPS URLs that don't match unsupported patterns
        // These might be direct media URLs or streaming URLs that are supported
        if (uri.Scheme == "http" || uri.Scheme == "https")
            return true;

        // For other schemes, be conservative and return false
        return false;
    }

    /// <summary>
    /// Checks if the URL matches known unsupported streaming platforms
    /// </summary>
    /// <param name="uri">The URI to check</param>
    /// <returns>True if the URL is from an unsupported streaming platform</returns>
    public static bool IsUnsupportedStreamingUrl(Uri uri)
    {
        if (uri == null || !uri.IsAbsoluteUri)
            return false;

        string host = uri.Host.ToLowerInvariant();
        
        return UnsupportedUrlPatterns.Any(pattern => 
            host.Equals(pattern, StringComparison.OrdinalIgnoreCase) || 
            host.EndsWith("." + pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the URL has a supported media file extension
    /// </summary>
    /// <param name="uri">The URI to check</param>
    /// <returns>True if the URL has a supported media extension</returns>
    private static bool HasSupportedMediaExtension(Uri uri)
    {
        try
        {
            string path = uri.AbsolutePath;
            string extension = Path.GetExtension(path);
            return !string.IsNullOrEmpty(extension) && SupportedMediaExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a user-friendly error message for unsupported URLs
    /// </summary>
    /// <param name="uri">The unsupported URI</param>
    /// <returns>A descriptive error message</returns>
    public static string GetUnsupportedUrlMessage(Uri uri)
    {
        if (uri == null)
            return "Invalid URL provided.";

        if (IsUnsupportedStreamingUrl(uri))
        {
            string host = uri.Host.ToLowerInvariant();
            if (host.Contains("youtube"))
                return "YouTube URLs are not supported. Please use direct media file URLs instead.";
            if (host.Contains("vimeo"))
                return "Vimeo URLs are not supported. Please use direct media file URLs instead.";
            if (host.Contains("twitch"))
                return "Twitch URLs are not supported. Please use direct media file URLs instead.";
            
            return "This streaming platform is not supported. Please use direct media file URLs instead.";
        }

        return "This URL format is not supported. Please use direct links to media files (MP4, MP3, M3U8, etc.).";
    }
}