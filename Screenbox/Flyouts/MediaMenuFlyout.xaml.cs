using System.Collections.Generic;
using System.Windows.Input;
using Screenbox.Behaviors;
using Screenbox.Commands;
using Screenbox.Converters;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Flyouts;

/// <summary>
/// Provides a context menu for media items with shared playback and management actions.
/// </summary>
/// <remarks>
/// The flyout binds to a single <see cref="ContextItem"/> and resolves either a <see cref="MediaViewModel"/>
/// directly or the wrapped media from a <see cref="StorageItemViewModel"/>.
/// <para>Additional actions can be injected through <see cref="AdditionalItems"/>
/// and are inserted after the properties action when the menu opens.</para>
/// </remarks>
public sealed partial class MediaMenuFlyout : MenuFlyout
{
    private static readonly bool _isApiContract14Present
        = Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 14);

    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="ContextItem"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContextItemProperty = DependencyProperty.Register(
        nameof(ContextItem), typeof(object), typeof(MediaMenuFlyout), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the media item associated with the current flyout.
    /// </summary>
    /// <value>The media item or storage item that the flyout should act on.</value>
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

    /// <summary>
    /// Gets or sets the command to invoke when the play item is tapped.
    /// </summary>
    /// <value>The command to invoke when the play item is tapped.</value>
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

    /// <summary>
    /// Gets or sets the command to invoke when the play next item is tapped.
    /// </summary>
    /// <value>The command to invoke when the play next item is tapped.</value>
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

    /// <summary>
    /// Gets or sets the command to invoke when the add to queue item is tapped.
    /// </summary>
    /// <value>The command to invoke when the add to queue item is tapped.</value>
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

    /// <summary>
    /// Gets or sets the command to invoke when the remove item is tapped.
    /// </summary>
    /// <value>The command to invoke when the remove item is tapped.</value>
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

    /// <summary>
    /// Gets or sets the command to invoke when the select item is tapped.
    /// </summary>
    /// <value>The command to invoke when the select item is tapped.</value>
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

    /// <summary>
    /// Gets or sets a value that indicates whether advanced playback options are enabled.
    /// </summary>
    /// <value><see langword="true"/> if advanced playback options should be displayed; otherwise, <see langword="false"/>.</value>
    public bool IsAdvancedModeEnabled
    {
        get { return (bool)GetValue(IsAdvancedModeEnabledProperty); }
        set { SetValue(IsAdvancedModeEnabledProperty, value); }
    }

    #endregion

    /// <summary>
    /// Gets the collection used to generate the additional content of the menu.
    /// </summary>
    /// <value>
    /// The collection that is used to generate the additional content of the menu.
    /// The default is an empty collection.
    /// </value>
    /// <remarks>
    /// When the menu opens, each item is placed in order, directly following the
    /// properties item.
    /// </remarks>
    public IList<MenuFlyoutItemBase> AdditionalItems { get; }

    private readonly SetPlaybackOptionsCommand _setPlaybackOptionsCommand = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaMenuFlyout"/> class.
    /// </summary>
    public MediaMenuFlyout()
    {
        this.InitializeComponent();

        AdditionalItems = new List<MenuFlyoutItemBase>();
    }

    private void OnOpening(object sender, object e)
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
        AddToPlaylistSubItemIcon.Glyph = !_isApiContract14Present
            ? GetGlyphForTextDirection("\U000F00AA", "\U000F00AB")
            : "\U000F00AA";

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
        if (!DeviceInfoHelper.IsXbox)
        {
            OpenInFileExplorerItem.Text = Strings.Resources.OpenInFileExplorer;
            OpenInFileExplorerItem.CommandParameter = mediaVm;
            OpenInFileExplorerItem.Visibility = Visibility.Visible;
            OpenInFileExplorerItemKeyboardAccelerator.IsEnabled = true;
        }

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

    private void OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        if (args.Cancel)
            return;

        if (!DeviceInfoHelper.IsXbox)
        {
            OpenInFileExplorerItemKeyboardAccelerator.IsEnabled = false;
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

    /// <summary>
    /// Returns the glyph that matches the current text direction.
    /// </summary>
    /// <param name="leftToRightGlyph">The glyph to use in left-to-right layouts.</param>
    /// <param name="rightToLeftGlyph">The glyph to use in right-to-left layouts.</param>
    /// <returns>The glyph appropriate for the current UI language direction.</returns>
    private static string GetGlyphForTextDirection(string leftToRightGlyph, string rightToLeftGlyph)
    {
        return GlobalizationHelper.IsRightToLeftLanguage ? rightToLeftGlyph : leftToRightGlyph;
    }
}
