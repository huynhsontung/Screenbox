#nullable enable

using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Templates;

/// <summary>
/// Selects a <see cref="DataTemplate"/> based on the type of a <see cref="SearchSuggestion"/>.
/// </summary>
internal sealed class SearchSuggestionTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template used for displaying a search suggestion item.
    /// </summary>
    /// <value>
    /// A <see cref="DataTemplate"/> that defines the appearance of a search suggestion item.
    /// </value>
    public DataTemplate? ItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used when there are no search suggestions.
    /// </summary>
    /// <value>
    /// A <see cref="DataTemplate"/> that defines the appearance of an empty search suggestion.
    /// </value>
    public DataTemplate? EmptyTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item is SearchSuggestion { Type: SearchSuggestionType.None }
            ? EmptyTemplate
            : ItemTemplate;
    }
}
