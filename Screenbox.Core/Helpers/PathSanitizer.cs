using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Screenbox.Core.Helpers;

/// <summary>
/// Provides utility methods for sanitizing file paths and extracting extensions or file types
/// to prevent exposing personally identifiable information (PII) or sensitive directory structures.
/// </summary>
public static partial class PathSanitizer
{
    // Matches Windows drive paths (C:\...), UNC paths (\\...), or file URIs (file:///...)
    // Path segments allow letters, numbers, and common path symbols. Spaces are only matched
    // when within a folder/file segment (e.g. followed by a slash or dot/alphanumeric before slash).
    [GeneratedRegex("""(?:[a-zA-Z]:[\\/]|\\\\|\bfile:\/\/\/)[^\s"'<>|]+""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FilePathRegex();

    /// <summary>
    /// Gets the file extension or file type for a path or URI string, or an empty string if none is available.
    /// </summary>
    /// <param name="path">The file path, URI, or file name to inspect.</param>
    /// <returns>
    /// The normalized extension in lower-case with leading dot (e.g. <c>".mp4"</c>),
    /// or <see cref="string.Empty"/> if not determinable.
    /// </returns>
    public static string GetExtensionOrType(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            string candidate = path.Trim();

            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri) && !uri.IsFile)
            {
                candidate = uri.AbsolutePath;
            }

            string extension = Path.GetExtension(candidate);
            return extension.ToLowerInvariant();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Redacts file paths within an arbitrary text string, replacing each detected file path
    /// with its extension or a redacted token.
    /// </summary>
    /// <param name="text">The message or text containing potential file paths.</param>
    /// <returns>The sanitized string with all file paths replaced.</returns>
    public static string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return FilePathRegex().Replace(text, match =>
        {
            string matchedPath = match.Value;
            string ext = GetExtensionOrType(matchedPath);
            return string.IsNullOrEmpty(ext) ? "[redacted_path]" : $"[file: {ext}]";
        });
    }
}
