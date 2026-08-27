using System.Collections.Generic;
using System.Windows.Input;
using Screenbox.Behaviors;
using Screenbox.Commands;
using Screenbox.Converters;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Flyouts;

public sealed partial class MediaMenuFlyout : MenuFlyout
{
    /// <summary>
    /// Identifies the <see cref="ContextItem"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContextItemProperty = DependencyProperty.Register(
        nameof(ContextItem), typeof(object), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public object? ContextItem
    {
        get { return (object?)GetValue(ContextItemProperty); }
        set { SetValue(ContextItemProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PlayCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
        nameof(PlayCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public ICommand? PlayCommand
    {
        get { return (ICommand?)GetValue(PlayCommandProperty); }
        set { SetValue(PlayCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="PlayNextCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlayNextCommandProperty = DependencyProperty.Register(
        nameof(PlayNextCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public ICommand? PlayNextCommand
    {
        get { return (ICommand?)GetValue(PlayNextCommandProperty); }
        set { SetValue(PlayNextCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="AddToQueueCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AddToQueueCommandProperty = DependencyProperty.Register(
        nameof(AddToQueueCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public ICommand? AddToQueueCommand
    {
        get { return (ICommand?)GetValue(AddToQueueCommandProperty); }
        set { SetValue(AddToQueueCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="RemoveCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty RemoveCommandProperty = DependencyProperty.Register(
        nameof(RemoveCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public ICommand? RemoveCommand
    {
        get { return (ICommand?)GetValue(RemoveCommandProperty); }
        set { SetValue(RemoveCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="SelectCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
        nameof(SelectCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    public ICommand? SelectCommand
    {
        get { return (ICommand?)GetValue(SelectCommandProperty); }
        set { SetValue(SelectCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsAdvancedModeEnabled"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsAdvancedModeEnabledProperty =
        DependencyProperty.Register(nameof(IsAdvancedModeEnabled), typeof(bool), typeof(MediaMenuFlyout), new PropertyMetadata(false));

    public bool IsAdvancedModeEnabled
    {
        get { return (bool)GetValue(IsAdvancedModeEnabledProperty); }
        set { SetValue(IsAdvancedModeEnabledProperty, value); }
    }

    public IList<MenuFlyoutItemBase> AdditionalItems { get; }

    private readonly SetPlaybackOptionsCommand _setPlaybackOptionsCommand = new();

    public MediaMenuFlyout()
    {
        this.InitializeComponent();

        AdditionalItems = new List<MenuFlyoutItemBase>();
    }

    private void MenuFlyout_Opening(object sender, object e)
    {
        UpdateAdditionalItems();

        // Resolve the underlying MediaViewModel regardless of whether ContextItem is a
        // MediaViewModel directly or a StorageItemViewModel wrapping one.
        var mediaVm = ContextItem switch
        {
            MediaViewModel media => media,
            StorageItemViewModel storageItem => storageItem.Media,
            _ => null,
        };

        // MenuFlyout Behavior 
        AddToPlaylistFlyoutBehavior.TargetSubItem = AddToPlaylistSubItem;

        // Play MenuFlyoutItem
        PlayItem.Command = PlayCommand;
        PlayItem.CommandParameter = ContextItem;
        PlayItem.Text = ItemLabelHelper.GetPlayPauseLabel(mediaVm?.IsPlaying ?? false);
        PlayItemIcon.Glyph = GlyphConverter.ToPlayPauseGlyph(mediaVm?.IsPlaying ?? false);

        // PlayNext MenuFlyoutItem
        PlayNextItem.Text = Strings.Resources.PlayNext;
        PlayNextItem.Command = PlayNextCommand;
        PlayNextItem.CommandParameter = ContextItem;

        // AddToQueue MenuFlyoutItem
        AddToQueueItem.Text = Strings.Resources.AddToQueue;
        AddToQueueItemIcon.Glyph = GetGlyphForTextDirection("\U000F00C2", "\U000F00C3");

        if (AddToQueueCommand is not null)
        {
            AddToQueueItem.Command = AddToQueueCommand;
            AddToQueueItem.CommandParameter = ContextItem;
            AddToQueueItem.Visibility = Visibility.Visible;
        }

        // AddToPlaylist MenuFlyoutSubItem
        AddToPlaylistSubItem.Text = Strings.Resources.AddToPlaylist;
        AddToPlaylistSubItemIcon.Glyph = GetGlyphForTextDirection("\U000F00AA", "\U000F00AB");

        // Remove MenuFlyoutItem
        RemoveItem.Text = Strings.Resources.Remove;

        if (RemoveCommand is not null)
        {
            RemoveItem.Command = RemoveCommand;
            RemoveItem.CommandParameter = ContextItem;
            RemoveItem.Visibility = Visibility.Visible;
        }

        // OpenWith MenuFlyoutItem
        OpenWithItem.Text = Strings.Resources.OpenWith;
        OpenWithItem.CommandParameter = mediaVm;

        // OpenInFileExplorer MenuFlyoutItem
        OpenInFileExplorerItem.Text = Strings.Resources.OpenInFileExplorer;
        OpenInFileExplorerItem.CommandParameter = mediaVm;
        OpenInFileExplorerItem.Visibility = DeviceInfoHelper.IsXbox ? Visibility.Collapsed : Visibility.Visible;

        // ShowProperties MenuFlyoutItem
        PropertiesItem.Text = Strings.Resources.Properties;
        PropertiesItem.CommandParameter = mediaVm;

        // Select MenuFlyoutItem
        SelectionItem.Text = Strings.Resources.Select;
        SelectionItemIcon.Glyph = GetGlyphForTextDirection("\uEA20", "\uEA66");

        if (SelectCommand is not null)
        {
            SelectionSeparator.Visibility = Visibility.Visible;

            SelectionItem.Command = SelectCommand;
            SelectionItem.CommandParameter = ContextItem;
            SelectionItem.Visibility = Visibility.Visible;
        }

        // Advanced PlaybackOptions MenuFlyoutItem
        SetPlaybackOptionsItem.Text = Strings.Resources.SetPlaybackOptions;
        if (IsAdvancedModeEnabled)
        {
            AdvancedModeSeparator.Visibility = Visibility.Visible;

            _setPlaybackOptionsCommand.PlayCommand = PlayCommand;
            SetPlaybackOptionsItem.Command = _setPlaybackOptionsCommand;
            SetPlaybackOptionsItem.CommandParameter = ContextItem;
            SetPlaybackOptionsItem.Visibility = Visibility.Visible;
        }
    }

    private void UpdateAdditionalItems()
    {
        int propertiesIndex = Items.IndexOf(PropertiesItem);
        int insertIndex = propertiesIndex < 0 ? Items.Count : propertiesIndex + 1;

        foreach (var item in AdditionalItems)
        {
            if (Items.Contains(item))
            {
                continue;
            }

            Items.Insert(insertIndex, item);
            insertIndex++;
        }
    }

    private static string GetGlyphForTextDirection(string leftToRightGlyph, string rightToLeftGlyph)
    {
        return GlobalizationHelper.IsRightToLeftLanguage ? rightToLeftGlyph : leftToRightGlyph;
    }
}
