#nullable enable

using Screenbox.Casting.Models;

namespace Screenbox.Casting.Contracts;

/// <summary>
/// Determines if media can be direct-played by Chromecast.
/// </summary>
public interface ICastCompatibilityAnalyzer
{
    /// <summary>
    /// Evaluates compatibility without performing remux/transcode.
    /// </summary>
    CastCompatibilityResult Analyze(CastMediaSource source);
}
