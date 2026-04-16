#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Screenbox.Core.Services;
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
        _characterGroupings = string.IsNullOrWhiteSpace(ApplicationLanguages.PrimaryLanguageOverride)
            ? new CharacterGroupings()
            : new CharacterGroupings(ApplicationLanguages.PrimaryLanguageOverride);
        CharacterGroupLabels = _characterGroupings
            .Select(x => string.IsNullOrEmpty(x.Label) ? OtherGroupSymbol : x.Label)
            .Distinct()
            .ToList();
        MaxGroupLabelLength = CharacterGroupLabels.Max(x => x.Length);

        _characterGroupSet = new HashSet<string>(CharacterGroupLabels, StringComparer.Ordinal);
    }

    public static string GetCharacterGroupLabel(string name)
    {
        try
        {
            string? label = _characterGroupings.Lookup(name);
            if (string.IsNullOrEmpty(label) || !_characterGroupSet.Contains(label))
                return OtherGroupSymbol;

            return label;
        }
        catch (Exception e)
        {
            LogService.Log(e);
        }

        return OtherGroupSymbol;
    }
}
