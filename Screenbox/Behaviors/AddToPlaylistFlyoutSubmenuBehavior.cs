#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Interactivity;
using Screenbox.Controls;
using Screenbox.Core.Contexts;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Behaviors;

/// <summary>
/// Populates a named <see cref="MenuFlyoutSubItem"/> within a <see cref="MenuFlyout"/> with playlist actions.
/// </summary>
internal sealed class AddToPlaylistFlyoutSubmenuBehavior : Behavior<MenuFlyout>
{
    public static readonly DependencyProperty TargetSubItemNameProperty = DependencyProperty.Register(
        nameof(TargetSubItemName),
        typeof(string),
        typeof(AddToPlaylistFlyoutSubmenuBehavior),
        new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the x:Name of the <see cref="MenuFlyoutSubItem"/> that should be populated.
    /// </summary>
    public string TargetSubItemName
    {
        get => (string)GetValue(TargetSubItemNameProperty);
        set => SetValue(TargetSubItemNameProperty, value);
    }

    public IAsyncRelayCommand<MediaViewModel?> CreatePlaylistCommand { get; }

    private readonly PlaylistsContext _playlistsContext;

    public AddToPlaylistFlyoutSubmenuBehavior()
    {
        _playlistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
        CreatePlaylistCommand = new AsyncRelayCommand<MediaViewModel?>(CreatePlaylistAsync);
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

    private void AssociatedObjectOnOpening(object sender, object e)
    {
        PopulateMenu();
    }

    private void PopulateMenu()
    {
        if (string.IsNullOrWhiteSpace(TargetSubItemName))
        {
            return;
        }

        if (!TryFindSubItem(AssociatedObject.Items, TargetSubItemName, out var targetSubItem))
        {
            return;
        }

        MediaViewModel? clicked = targetSubItem.DataContext switch
        {
            StorageItemViewModel svm => svm.Media,
            MediaViewModel vm => vm,
            _ => null,
        };
        IReadOnlyList<MediaViewModel> clickedItems = clicked is not null
            ? [clicked]
            : Array.Empty<MediaViewModel>();

        targetSubItem.Items.Clear();
        targetSubItem.Items.Add(new MenuFlyoutItem
        {
            Icon = new SymbolIcon(Symbol.Add),
            Text = Strings.Resources.CreateNewPlaylist,
            Command = CreatePlaylistCommand,
            CommandParameter = clicked
        });

        targetSubItem.Items.Add(new MenuFlyoutSeparator());

        if (_playlistsContext.Playlists.Count == 0)
        {
            targetSubItem.Items.Add(new MenuFlyoutItem
            {
                Text = Strings.Resources.NoPlaylists,
                IsEnabled = false
            });
            return;
        }

        foreach (var playlist in _playlistsContext.Playlists.Where(p => p is not null))
        {
            targetSubItem.Items.Add(new MenuFlyoutItem
            {
                Text = playlist.Name,
                Command = playlist.AddItemsCommand,
                CommandParameter = clickedItems
            });
        }
    }

    private async Task CreatePlaylistAsync(MediaViewModel? parameter)
    {
        var playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (string.IsNullOrWhiteSpace(playlistName))
            return;

        var playlist = Ioc.Default.GetRequiredService<PlaylistViewModel>();
        playlist.Name = playlistName!;
        if (parameter != null)
        {
            playlist.Items.Add(parameter);
        }

        await playlist.SaveAsync();

        // Assume sort by last updated
        _playlistsContext.Playlists.Insert(0, playlist);
    }

    private static bool TryFindSubItem(IList<MenuFlyoutItemBase> items, string name, out MenuFlyoutSubItem subItem)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is MenuFlyoutSubItem candidate)
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    subItem = candidate;
                    return true;
                }

                if (candidate.Items is { Count: > 0 } && TryFindSubItem(candidate.Items, name, out subItem))
                {
                    return true;
                }
            }
        }

        subItem = null!;
        return false;
    }
}
