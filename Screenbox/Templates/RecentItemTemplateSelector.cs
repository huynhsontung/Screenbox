using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Templates;

/// <summary>
/// Selects a <see cref="DataTemplate"/> for an item in the Home page Recent Media grid.
/// </summary>
internal sealed partial class RecentItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MediaTemplate { get; set; }

    public DataTemplate? AlbumTemplate { get; set; }

    public DataTemplate? ArtistTemplate { get; set; }

    public DataTemplate? PlaylistTemplate { get; set; }

    public DataTemplate? MediaCollectionTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            AlbumViewModel => AlbumTemplate,
            ArtistViewModel => ArtistTemplate,
            PlaylistViewModel => PlaylistTemplate,
            MediaCollectionViewModel => MediaCollectionTemplate,
            RecentMediaItemViewModel => MediaTemplate,
            _ => base.SelectTemplateCore(item),
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
