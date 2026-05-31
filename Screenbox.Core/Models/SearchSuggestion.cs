#nullable enable

using Screenbox.Core.Enums;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents a search suggestion with its type, display text, and associated data.
/// </summary>
/// <param name="Type">A value of the enumeration that specifies the type of search suggestion.</param>
/// <param name="Text">The text to display for the search suggestion.</param>
/// <param name="Data">Optional data associated with the search suggestion. The default is <see langword="null"/>.</param>
public sealed record SearchSuggestion(SearchSuggestionType Type, string Text, object? Data = null)
{
    /// <summary>
    /// Gets the type of the search suggestion.
    /// </summary>
    /// <value>A value indicating the suggestion type.</value>
    public SearchSuggestionType Type { get; } = Type;

    /// <summary>
    /// Gets the text to display for the search suggestion.
    /// </summary>
    /// <value>The text content of the search suggestion.</value>
    public string Text { get; } = Text;

    /// <summary>
    /// Gets the data associated with the search suggestion.
    /// </summary>
    /// <value>
    /// An object containing data associated with the search suggestion,
    /// or <see langword="null"/> if no data is associated.
    /// </value>
    public object? Data { get; } = Data;
}
