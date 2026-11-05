using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Templates;

/// <summary>
/// Selects the appropriate <see cref="DataTemplate"/> for a <see cref="RecentItemViewModel"/> based on its <see cref="RecentItemKind"/>.
/// </summary>
public sealed class RecentItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template for single media items.
    /// </summary>
    public DataTemplate? MediaTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for ad-hoc and user playlists.
    /// </summary>
    public DataTemplate? PlaylistTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for albums.
    /// </summary>
    public DataTemplate? AlbumTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for artists.
    /// </summary>
    public DataTemplate? ArtistTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return SelectTemplateForItem(item);
    }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateForItem(item);
    }

    private DataTemplate? SelectTemplateForItem(object item)
    {
        if (item is RecentItemViewModel vm)
        {
            return vm.Kind switch
            {
                RecentItemKind.SingleMedia => MediaTemplate,
                RecentItemKind.AdHocPlaylist or RecentItemKind.UserPlaylist => PlaylistTemplate,
                RecentItemKind.Album => AlbumTemplate,
                RecentItemKind.Artist => ArtistTemplate,
                _ => MediaTemplate
            };
        }

        return MediaTemplate;
    }
}
