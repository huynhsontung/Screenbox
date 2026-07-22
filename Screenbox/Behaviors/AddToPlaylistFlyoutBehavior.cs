#nullable enable

using System;
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

    public AddToPlaylistFlyoutBehavior()
    {
        _playlistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
        CreatePlaylistCommand = new AsyncRelayCommand<IEnumerable<MediaViewModel>>(CreatePlaylistAsync);
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

        IReadOnlyList<MediaViewModel> contextItems = dataContext switch
        {
            StorageItemViewModel { Media: { } media } => [media],
            MediaViewModel vm => [vm],
            IReadOnlyList<MediaViewModel> list => list,
            IEnumerable<MediaViewModel> collection => collection.ToList(),
            IEnumerable<object> objects => objects.OfType<MediaViewModel>().ToList(),
            _ => Array.Empty<MediaViewModel>(),
        };

        menuItems.Clear();
        menuItems.Add(new MenuFlyoutItem
        {
            Icon = new SymbolIcon(Symbol.Add),
            Text = Strings.Resources.CreateNewPlaylist,
            Command = CreatePlaylistCommand,
            CommandParameter = contextItems
        });

        menuItems.Add(new MenuFlyoutSeparator());

        if (_playlistsContext.Playlists.Count == 0)
        {
            menuItems.Add(new MenuFlyoutItem
            {
                Text = Strings.Resources.NoPlaylists,
                IsEnabled = false
            });
            return;
        }

        foreach (var playlist in _playlistsContext.Playlists.Where(p => p is not null))
        {
            menuItems.Add(new MenuFlyoutItem
            {
                Text = playlist.Name,
                Command = playlist.AddItemsCommand,
                CommandParameter = contextItems
            });
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
