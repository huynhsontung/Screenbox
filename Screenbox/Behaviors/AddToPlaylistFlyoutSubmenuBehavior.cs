#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Xaml.Interactivity;
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

    private CommonViewModel Common { get; }
    private PlaylistsContext PlaylistsContext { get; }

    public AddToPlaylistFlyoutSubmenuBehavior()
    {
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        PlaylistsContext = Ioc.Default.GetRequiredService<PlaylistsContext>();
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

        var clicked = targetSubItem.DataContext as MediaViewModel;
        IReadOnlyList<MediaViewModel> clickedItems = clicked is not null
            ? [clicked]
            : Array.Empty<MediaViewModel>();

        targetSubItem.Items.Clear();

        if (clicked is not null)
        {
            string defaultName = string.IsNullOrWhiteSpace(clicked.Name)
                ? Strings.Resources.NewPlaylist
                : clicked.Name;

            targetSubItem.Items.Add(new MenuFlyoutItem
            {
                Text = Strings.Resources.CreateNewPlaylist,
                Command = Common.CreatePlaylistWithItemsCommand,
                CommandParameter = (defaultName, clickedItems)
            });

            targetSubItem.Items.Add(new MenuFlyoutSeparator());
        }

        if (PlaylistsContext.Playlists.Count == 0)
        {
            targetSubItem.Items.Add(new MenuFlyoutItem
            {
                Text = Strings.Resources.NoPlaylists,
                IsEnabled = false
            });
            return;
        }

        foreach (var playlist in PlaylistsContext.Playlists.Where(p => p is not null))
        {
            targetSubItem.Items.Add(new MenuFlyoutItem
            {
                Text = playlist.Caption,
                Command = Common.AddItemsToPlaylistCommand,
                CommandParameter = (playlist, clickedItems)
            });
        }
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
