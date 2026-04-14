#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Globalization.Collation;

namespace Screenbox.Core.Helpers;

public static class MediaGroupingHelpers
{
    public const string OtherGroupSymbol = "\u2026";

    public static readonly IReadOnlyList<string> CharacterGroupLabels;

    public static readonly int MaxGroupLabelLength;

    private static readonly CharacterGroupings _characterGroupings = new();

    private static readonly HashSet<string> _characterGroupSet;

    static MediaGroupingHelpers()
    {
        CharacterGroupLabels = [.. _characterGroupings
            .Select(x => string.IsNullOrEmpty(x.Label) ? OtherGroupSymbol : x.Label)
            .Distinct()];
        MaxGroupLabelLength = CharacterGroupLabels.Select(x => x.Length).Max();

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
        catch { }
        return OtherGroupSymbol;
    }
}
