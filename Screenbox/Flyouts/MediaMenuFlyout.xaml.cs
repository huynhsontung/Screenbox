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
    /// Identifies the <see cref="IsAdvancedModeEnabled"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsAdvancedModeEnabledProperty =
        DependencyProperty.Register(nameof(IsAdvancedModeEnabled), typeof(bool), typeof(MediaMenuFlyout), new PropertyMetadata(false));

    public bool IsAdvancedModeEnabled
    {
        get { return (bool)GetValue(IsAdvancedModeEnabledProperty); }
        set { SetValue(IsAdvancedModeEnabledProperty, value); }
    }

    public MediaMenuFlyout()
    {
        this.InitializeComponent();
    }

    private void MenuFlyout_Opening(object sender, object e)
    {
        var mediaVm = ContextItem as MediaViewModel;
        if (ContextItem is StorageItemViewModel storageItem)
        {
            mediaVm = storageItem.Media;
        }

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
        AddToQueueItem.Command = AddToQueueCommand;
        AddToQueueItem.CommandParameter = ContextItem;
        AddToQueueItemIcon.Glyph = GlobalizationHelper.IsRightToLeftLanguage ? "\U000F00C3" : "\U000F00C2";

        // AddToPlaylist MenuFlyoutSubItem
        AddToPlaylistSubItem.Text = Strings.Resources.AddToPlaylist;
        AddToPlaylistSubItemIcon.Glyph = GlobalizationHelper.IsRightToLeftLanguage ? "\U000F00AB" : "\U000F00AA";

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

        // Advanced MenuFlyoutSeparator
        AdvancedModeSeparator.Visibility = IsAdvancedModeEnabled ? Visibility.Visible : Visibility.Collapsed;

        // Advanced PlaybackOptions MenuFlyoutItem
        SetPlaybackOptionsItem.Text = Strings.Resources.SetPlaybackOptions;
        SetPlaybackOptionsItem.Command = new SetPlaybackOptionsCommand()
        {
            PlayCommand = PlayCommand,
        };
        SetPlaybackOptionsItem.CommandParameter = ContextItem;
        SetPlaybackOptionsItem.Visibility = IsAdvancedModeEnabled ? Visibility.Visible : Visibility.Collapsed;
    }
}
