using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Globalization;
using Windows.Globalization.Collation;

namespace Screenbox.Core.Helpers;

public static class MediaGroupingHelpers
{
    public const string OtherGroupSymbol = "\u2026";

    public static IReadOnlyList<string> CharacterGroupLabels { get; }

    public static int MaxGroupLabelLength { get; }

    private static readonly CharacterGroupings _characterGroupings;
    private static readonly HashSet<string> _characterGroupSet;

    static MediaGroupingHelpers()
    {
        string? overrideLanguage = null;
        try
        {
            overrideLanguage = ApplicationLanguages.PrimaryLanguageOverride;
        }
        catch (Exception)
        {
            // Unpackaged environment (e.g. unit tests) lacks package identity
        }

        _characterGroupings = string.IsNullOrWhiteSpace(overrideLanguage)
            ? new CharacterGroupings()
            : new CharacterGroupings(overrideLanguage);
        CharacterGroupLabels = _characterGroupings
            .Select(x => string.IsNullOrEmpty(x.Label) ? OtherGroupSymbol : x.Label)
            .Distinct()
            .ToList();
        MaxGroupLabelLength = CharacterGroupLabels.Max(x => x.Length);

        _characterGroupSet = new HashSet<string>(CharacterGroupLabels, StringComparer.Ordinal);
    }

    public static string GetCharacterGroupLabel(string name)
    {
        string? label = _characterGroupings.Lookup(name);
        return string.IsNullOrEmpty(label) || !_characterGroupSet.Contains(label)
            ? OtherGroupSymbol
            : label;
    }
}
