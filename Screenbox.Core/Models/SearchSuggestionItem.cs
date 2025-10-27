#nullable enable

using Screenbox.Core.Enums;

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
    /// Gets the category of the search suggestion.
    /// </summary>
    /// <value>The suggestion category.</value>
    public SearchSuggestionKind? Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestionItem"/> class.
    /// </summary>
    /// <param name="name">The text to display in the search suggestion list.</param>
    /// <param name="viewModel">The view model instance that this suggestion represents.</param>
    public SearchSuggestionItem(string name, object? viewModel = default, SearchSuggestionKind? type = default)
    {
        Name = name;
        Data = viewModel;
        Type = type;
    }
}
