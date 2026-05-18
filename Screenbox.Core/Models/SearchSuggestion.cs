#nullable enable

using System;
using System.Runtime.CompilerServices;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents a search suggestion item with a type, display text, and associated data.
/// </summary>
public sealed class SearchSuggestion : IEquatable<SearchSuggestion>
{
    /// <summary>
    /// Gets the type of the search suggestion.
    /// </summary>
    /// <value>
    /// A value of the <see cref="SearchSuggestionType"/> enumeration that specifies
    /// the suggestion type.
    /// </value>
    public SearchSuggestionType Type { get; }

    /// <summary>
    /// Gets the text of the suggestion for display.
    /// </summary>
    /// <value>The text to display.</value>
    public string Text { get; }

    /// <summary>
    /// Gets the data associated with the search suggestion.
    /// </summary>
    /// <value>
    /// An object containing additional data for the suggestion,
    /// or <see langword="null"/> if not set.
    /// </value>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestion"/> class
    /// with the type, specified display text, and optional data.
    /// </summary>
    /// <param name="type">A value indicating the suggestion type.</param>
    /// <param name="text">The display text of the search suggestion.</param>
    /// <param name="data">An optional object containing additional data for the suggestion. The default is <see langword="null"/>.</param>
    public SearchSuggestion(SearchSuggestionType type, string text, object? data = null)
    {
        Type = type;
        Text = text;
        Data = data;
    }

    /// <summary>
    /// Determines whether the specified <see cref="SearchSuggestion"/> object
    /// is equal to the current <see cref="SearchSuggestion"/> object.
    /// </summary>
    /// <param name="other">The <see cref="SearchSuggestion"/> object to be compared.</param>
    /// <returns>
    /// <see langword="true"/> if the two <see cref="SearchSuggestion"/> values are the same;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(SearchSuggestion? other)
    {
        return other is not null
            && string.Equals(Text, other.Text, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type
            && ReferenceEquals(Data, other.Data);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SearchSuggestion other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Type.GetHashCode();
            hash = hash * 29 + StringComparer.OrdinalIgnoreCase.GetHashCode(Text);
            hash = hash * 31 + (Data is null ? 0 : RuntimeHelpers.GetHashCode(Data));
            return hash;
        }
    }

    /// <summary>
    /// Determines whether two <see cref="SearchSuggestion"/> instances have the same value.
    /// </summary>
    /// <param name="s1">A <see cref="SearchSuggestion"/> to compare with <paramref name="s2"/>.</param>
    /// <param name="s2">A <see cref="SearchSuggestion"/> to compare with <paramref name="s1"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the <see cref="SearchSuggestion"/> instances are equivalent;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(SearchSuggestion? s1, SearchSuggestion? s2)
    {
        if (ReferenceEquals(s1, s2))
        {
            return true;
        }

        if (s1 is null || s2 is null)
        {
            return false;
        }

        return s1.Equals(s2);
    }

    /// <summary>
    /// Determines whether two <see cref="SearchSuggestion"/> instances do not have the same value.
    /// </summary>
    /// <param name="s1">A <see cref="SearchSuggestion"/> to compare with <paramref name="s2"/>.</param>
    /// <param name="s2">A <see cref="SearchSuggestion"/> to compare with <paramref name="s1"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the two <see cref="SearchSuggestion"/> instances are not equal;
    /// otherwise, <see langword="false"/>. If either parameter is <see langword="null"/>,
    /// this method returns <see langword="true"/>.
    /// </returns>
    public static bool operator !=(SearchSuggestion? s1, SearchSuggestion? s2)
    {
        if (ReferenceEquals(s1, s2))
        {
            return false;
        }

        if (s1 is null || s2 is null)
        {
            return true;
        }

        return !s1.Equals(s2);
    }
}
