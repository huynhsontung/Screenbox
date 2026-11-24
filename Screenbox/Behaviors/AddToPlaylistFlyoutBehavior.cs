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
        [DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
        get => (MenuFlyoutSubItem?)GetValue(TargetSubItemProperty);
        set => SetValue(TargetSubItemProperty, value);
    }

    private IAsyncRelayCommand<object[]> CreatePlaylistCommand { get; }

    private IAsyncRelayCommand<(PlaylistViewModel Playlist, object[] Targets)> AddToPlaylistCommand { get; }

    private readonly PlaylistsContext _playlistsContext;
    private FrameworkElement? _flyoutTarget;

    public AddToPlaylistFlyoutBehavior()
    {
        _playlistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
        CreatePlaylistCommand = new AsyncRelayCommand<object[]>(CreatePlaylistAsync);
        AddToPlaylistCommand = new AsyncRelayCommand<(PlaylistViewModel Playlist, object[] Targets)>(AddToPlaylistAsync);
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

        object[] targets = SnapshotTargets(dataContext);

        menuItems.Clear();
        menuItems.Add(new MenuFlyoutItem
        {
            Icon = new SymbolIcon(Symbol.Add),
            Text = Strings.Resources.CreateNewPlaylist,
            Command = CreatePlaylistCommand,
            CommandParameter = targets
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
                Command = AddToPlaylistCommand,
                CommandParameter = (playlist, targets)
            });
        }
    }

    private static object[] SnapshotTargets(object? dataContext) => dataContext switch
    {
        StorageItemViewModel { Media: { } media } => [media],
        MediaViewModel vm => [vm],
        IMediaContainer container => [container.Media],
        AlbumViewModel album => [album],
        ArtistViewModel artist => [artist],
        PlaylistViewModel playlist => [playlist],
        MediaCollectionViewModel collection => [collection],
        SelectionViewModel selection => selection.GetSelectedItems<object>().ToArray(),
        IReadOnlyList<MediaViewModel> list => list.Cast<object>().ToArray(),
        IEnumerable<MediaViewModel> collection => collection.Cast<object>().ToArray(),
        IEnumerable<IMediaContainer> containers => containers.Select(c => (object)c.Media).ToArray(),
        IEnumerable<object> objects => objects.ToArray(),
        _ => [],
    };

    private static async Task<MediaViewModel[]> ResolveMediaItemsAsync(object[]? targets)
    {
        if (targets is null or { Length: 0 })
            return [];

        var resolved = new List<MediaViewModel>();
        foreach (object target in targets)
        {
            switch (target)
            {
                case MediaViewModel vm:
                    resolved.Add(vm);
                    break;
                case IMediaContainer container:
                    resolved.Add(container.Media);
                    break;
                case StorageItemViewModel { Media: { } media }:
                    resolved.Add(media);
                    break;
                case AlbumViewModel album:
                    resolved.AddRange(album.OrderedSongs);
                    break;
                case ArtistViewModel artist:
                    resolved.AddRange(artist.OrderedSongs);
                    break;
                case PlaylistViewModel playlist:
                    resolved.AddRange(playlist.Items.Select(item => item.Media));
                    break;
                case MediaCollectionViewModel collection:
                    try
                    {
                        resolved.AddRange(await collection.GetPlayableItemsAsync());
                    }
                    catch
                    {
                        // Best-effort: skip collections that can no longer be resolved from disk/URI.
                    }
                    break;
            }
        }

        return resolved.ToArray();
    }

    private async Task AddToPlaylistAsync((PlaylistViewModel Playlist, object[] Targets) args)
    {
        MediaViewModel[] itemsToAdd = await ResolveMediaItemsAsync(args.Targets);
        if (itemsToAdd.Length > 0 && args.Playlist.AddItemsCommand.CanExecute(itemsToAdd))
        {
            args.Playlist.AddItemsCommand.Execute(itemsToAdd);
        }
    }

    private async Task CreatePlaylistAsync(object[]? targets)
    {
        var playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (string.IsNullOrWhiteSpace(playlistName))
            return;

        MediaViewModel[] itemsToAdd = await ResolveMediaItemsAsync(targets);
        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = playlistName!;
        foreach (var item in itemsToAdd)
        {
            playlist.Items.Add(new PlaylistItemViewModel(item));
        }

        await playlist.SaveAsync();

        // Assume sort by last updated
        _playlistsContext.Playlists.Insert(0, playlist);
    }
}
