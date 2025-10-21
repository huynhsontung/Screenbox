#nullable enable

namespace Screenbox.Core.Models;

/// <summary>
/// Represents a search suggestion item with display information and the underlying model.
/// </summary>
public sealed class SearchSuggestionItem
{
    /// <summary>
    /// Gets the display name of the search suggestion.
    /// </summary>
    /// <value>The user-facing text displayed in the search suggestion list.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the underlying view model object associated with this search suggestion.
    /// </summary>
    /// <value>The data view model instance (<see cref="ViewModels.MediaViewModel"/>, <see cref="ViewModels.AlbumViewModel"/>, or <see cref="ViewModels.ArtistViewModel"/>).</value>
    public object? Data { get; }

    /// <summary>
    /// Gets the character code that identifies the suggestion icon.
    /// </summary>
    /// <value>The hexadecimal character code for the suggestion icon glyph.</value>
    public string? Glyph { get; }

    /// <summary>
    /// Gets the display type of the search suggestion item.
    /// </summary>
    /// <value>The user-facing text for screen reader use to describe the type of the search suggestion item.</value>
    public string? Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestionItem"/> class.
    /// </summary>
    /// <param name="name">The text to display in the search suggestion list.</param>
    /// <param name="viewModel">The view model instance that this suggestion represents.</param>
    /// <param name="glyph">The hexadecimal character code that visually identifies the suggestion type.</param>
    /// <param name="type">The accessible name for the type of the search suggestion item.</param>
    public SearchSuggestionItem(string name, object? viewModel = default, string? glyph = default, string? type = null)
    {
        Name = name;
        Data = viewModel;
        Glyph = glyph;
        Type = type;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? string.Empty : $"{Type}; {Name}";
    }
}
