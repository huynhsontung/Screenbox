using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Interactivity;
using Screenbox.Core.Contexts;
using Screenbox.Core.ViewModels;
using Screenbox.Dialogs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Behaviors;

/// <summary>
/// Represents a behavior that populates a <see cref="MenuFlyout"/>, or a target <see cref="MenuFlyoutSubItem"/>,
/// with playlist actions, allowing users to add media items to existing playlists or create new playlists.
/// </summary>
internal sealed partial class AddToPlaylistFlyoutBehavior : Behavior<MenuFlyout>
{
    public static readonly DependencyProperty DataContextProperty = DependencyProperty.Register(
        nameof(DataContext),
        typeof(object),
        typeof(AddToPlaylistFlyoutBehavior),
        new PropertyMetadata(null));

    public object? DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="TargetSubItem"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TargetSubItemProperty = DependencyProperty.Register(
        nameof(TargetSubItem),
        typeof(MenuFlyoutSubItem),
        typeof(AddToPlaylistFlyoutBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the target <see cref="MenuFlyoutSubItem"/> to populate with playlist actions.
    /// </summary>
    /// <remarks>
    /// If not specified, the root level of the <see cref="MenuFlyout"/> will be populated.
    /// </remarks>
    /// <value>The target <see cref="MenuFlyoutSubItem"/>. The default is <see langword="null"/>.</value>
    public MenuFlyoutSubItem? TargetSubItem
    {
        get => (MenuFlyoutSubItem?)GetValue(TargetSubItemProperty);
        set => SetValue(TargetSubItemProperty, value);
    }

    private IAsyncRelayCommand<IEnumerable<MediaViewModel>> CreatePlaylistCommand { get; }

    private readonly PlaylistsContext _playlistsContext;
    private FrameworkElement? _flyoutTarget;
    private readonly MenuFlyoutItem _createNewPlaylistItem;
    private readonly MenuFlyoutSeparator _separator = new();
    private readonly MenuFlyoutItem _noPlaylistsItem;
    private readonly List<MenuFlyoutItem> _playlistItems = new();

    public AddToPlaylistFlyoutBehavior()
    {
        _playlistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
        CreatePlaylistCommand = new AsyncRelayCommand<IEnumerable<MediaViewModel>>(CreatePlaylistAsync);

        _createNewPlaylistItem = new MenuFlyoutItem
        {
            Icon = new SymbolIcon(Symbol.Add),
            Text = Strings.Resources.CreateNewPlaylist,
            Command = CreatePlaylistCommand
        };

        _noPlaylistsItem = new MenuFlyoutItem
        {
            Text = Strings.Resources.NoPlaylists,
            IsEnabled = false
        };
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        // Populate each time the flyout opens, so it reflects the latest playlists.
        AssociatedObject.Opening += AssociatedObjectOnOpening;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Opening -= AssociatedObjectOnOpening;
    }

    private void AssociatedObjectOnOpening(object? sender, object e)
    {
        _flyoutTarget = AssociatedObject.Target;
        PopulateMenu();
    }

    private void PopulateMenu()
    {
        // The DataContext set at behavior level takes precedence, then the DataContext of the target sub-menu (if specified),
        // and finally the DataContext of the element
        object? dataContext = DataContext;

        // If a TargetSubItem is specified, we want to populate that sub-menu instead of the root level of the flyout.
        IList<MenuFlyoutItemBase> menuItems = TargetSubItem?.Items ?? AssociatedObject.Items;

        // If no DataContext is set at the behavior or sub-menu level, we can try to fall back to the target element's DataContext
        dataContext ??= _flyoutTarget?.DataContext;

        MediaViewModel[] contextItems = dataContext switch
        {
            StorageItemViewModel { Media: { } media } => [media],
            MediaViewModel vm => [vm],
            IReadOnlyList<MediaViewModel> list => list.ToArray(),
            IEnumerable<MediaViewModel> collection => collection.ToArray(),
            IEnumerable<object> objects => objects.OfType<MediaViewModel>().ToArray(),
            _ => [],
        };

        _createNewPlaylistItem.CommandParameter = contextItems;

        var playlists = _playlistsContext.Playlists.Where(p => p is not null).ToList();

        if (playlists.Count == 0)
        {
            if (menuItems.Count != 3 || menuItems[0] != _createNewPlaylistItem || menuItems[2] != _noPlaylistsItem)
            {
                menuItems.Clear();
                menuItems.Add(_createNewPlaylistItem);
                menuItems.Add(_separator);
                menuItems.Add(_noPlaylistsItem);
            }
            return;
        }

        while (_playlistItems.Count < playlists.Count)
        {
            _playlistItems.Add(new MenuFlyoutItem());
        }

        for (int i = 0; i < playlists.Count; i++)
        {
            var playlist = playlists[i];
            var item = _playlistItems[i];
            item.Text = playlist.Name;
            item.Command = playlist.AddItemsCommand;
            item.CommandParameter = contextItems;
        }

        int expectedCount = 2 + playlists.Count;
        bool needsRebuild = menuItems.Count != expectedCount ||
                            menuItems[0] != _createNewPlaylistItem ||
                            menuItems[1] != _separator;

        if (!needsRebuild)
        {
            for (int i = 0; i < playlists.Count; i++)
            {
                if (menuItems[i + 2] != _playlistItems[i])
                {
                    needsRebuild = true;
                    break;
                }
            }
        }

        if (needsRebuild)
        {
            menuItems.Clear();
            menuItems.Add(_createNewPlaylistItem);
            menuItems.Add(_separator);
            for (int i = 0; i < playlists.Count; i++)
            {
                menuItems.Add(_playlistItems[i]);
            }
        }
    }

    private async Task CreatePlaylistAsync(IEnumerable<MediaViewModel>? itemsToAdd)
    {
        var playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (string.IsNullOrWhiteSpace(playlistName))
            return;

        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = playlistName!;
        if (itemsToAdd != null)
        {
            foreach (var item in itemsToAdd)
            {
                playlist.Items.Add(item);
            }
        }

        await playlist.SaveAsync();

        // Assume sort by last updated
        _playlistsContext.Playlists.Insert(0, playlist);
    }
}
