#nullable enable

namespace Screenbox.Casting.Models;

public enum CastCompatibility
{
    DirectPlay = 0,
    RequiresRemuxOrTranscode = 1,
}

/// <summary>
/// Captures media compatibility preflight result for Chromecast.
/// </summary>
public sealed class CastCompatibilityResult
{
    /// <summary>
    /// Initializes a new <see cref="CastCompatibilityResult"/> instance.
    /// </summary>
    public CastCompatibilityResult(CastCompatibility compatibility, string reason)
    {
        Compatibility = compatibility;
        Reason = reason;
    }

    public CastCompatibility Compatibility { get; }

    public string Reason { get; }
}
