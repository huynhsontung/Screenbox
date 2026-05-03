#nullable enable

using System;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;

namespace Screenbox.Casting.Services;

/// <summary>
/// Basic compatibility analyzer that blocks known remux/transcode paths.
/// </summary>
public sealed class BasicCastCompatibilityAnalyzer : ICastCompatibilityAnalyzer
{
    /// <summary>
    /// Evaluates direct-play compatibility using content-type rules.
    /// </summary>
    public CastCompatibilityResult Analyze(CastMediaSource source)
    {
        string contentType = source.ContentType.Trim().ToLowerInvariant();

        // This intentionally remains conservative until remux/transcode approval is given.
        if (contentType.StartsWith("video/mp4", StringComparison.Ordinal) ||
            contentType.StartsWith("audio/mp4", StringComparison.Ordinal) ||
            contentType.StartsWith("audio/mpeg", StringComparison.Ordinal) ||
            contentType.StartsWith("application/vnd.apple.mpegurl", StringComparison.Ordinal))
        {
            return new CastCompatibilityResult(CastCompatibility.DirectPlay, "Compatible for direct play.");
        }

        return new CastCompatibilityResult(CastCompatibility.RequiresRemuxOrTranscode, "Source is not guaranteed to be Chromecast-compatible without remux/transcode.");
    }
}
