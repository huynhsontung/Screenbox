#nullable enable

namespace Screenbox.Core.Models;

/// <summary>
/// Represents a language with its native name and language tag.
/// </summary>
/// <param name="NativeName">The name of the language in the language itself.</param>
/// <param name="LanguageTag">The normalized BCP-47 language tag.</param>
public sealed record LanguageInfo(string NativeName, string LanguageTag)
{
    /// <inheritdoc cref="Windows.Globalization.Language.NativeName"/>
    public string NativeName { get; set; } = NativeName;

    /// <inheritdoc cref="Windows.Globalization.Language.LanguageTag"/>
    public string LanguageTag { get; set; } = LanguageTag;
}
