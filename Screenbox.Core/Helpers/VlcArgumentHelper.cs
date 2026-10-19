using System;
using System.Linq;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Provides utility methods for parsing and sanitizing LibVLC command-line arguments.
/// </summary>
public static class VlcArgumentHelper
{
    /// <summary>
    /// Sanitizes command-line arguments for LibVLC by trimming whitespace,
    /// filtering out non-option tokens and lone dashes, and returning space-delimited options.
    /// </summary>
    /// <param name="raw">The raw input string of arguments.</param>
    /// <returns>A space-delimited string of valid command-line options.</returns>
    public static string SanitizeArguments(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith('-') && s != "--")
            .ToArray();

        return string.Join(' ', args);
    }
}
