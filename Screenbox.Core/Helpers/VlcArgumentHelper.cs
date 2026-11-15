using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        string[] args = Tokenize(raw, keepQuotes: true)
            .Where(s => s.StartsWith('-') && s != "--")
            .ToArray();

        return string.Join(' ', args);
    }

    /// <summary>
    /// Splits a raw argument string into individual arguments, treating double-quoted
    /// sections as a single group. The grouping quotes are removed from the result.
    /// </summary>
    /// <param name="raw">The raw input string of arguments.</param>
    /// <returns>The list of parsed arguments.</returns>
    public static IReadOnlyList<string> ParseArguments(string raw)
    {
        return Tokenize(raw, keepQuotes: false);
    }

    /// <summary>
    /// Joins parsed arguments into a single string that <see cref="ParseArguments"/> can parse back,
    /// double-quoting any argument that contains whitespace.
    /// </summary>
    /// <param name="args">The parsed arguments.</param>
    /// <returns>A space-delimited string of arguments.</returns>
    public static string FormatArguments(IEnumerable<string> args)
    {
        return string.Join(' ', args.Select(a => a.Any(char.IsWhiteSpace) ? $"\"{a}\"" : a));
    }

    private static List<string> Tokenize(string raw, bool keepQuotes)
    {
        List<string> tokens = new();
        if (string.IsNullOrWhiteSpace(raw)) return tokens;

        StringBuilder current = new();
        bool inQuotes = false;
        bool hasToken = false;
        foreach (char c in raw)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
                if (keepQuotes) current.Append(c);
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasToken)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken) tokens.Add(current.ToString());
        return tokens;
    }
}
