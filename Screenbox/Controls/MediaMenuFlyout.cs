using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.WinUI;
using Microsoft.Xaml.Interactivity;
using Screenbox.Commands;
using Screenbox.Converters;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Screenbox.Strings;

namespace Screenbox.Controls;

public partial class MediaMenuFlyout : MenuFlyout
{
    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
        nameof(PlayCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));
    public ICommand PlayCommand { get => (ICommand)GetValue(PlayCommandProperty); set => SetValue(PlayCommandProperty, value); }

    public static readonly DependencyProperty PlayNextCommandProperty = DependencyProperty.Register(
        nameof(PlayNextCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));
    public ICommand PlayNextCommand { get => (ICommand)GetValue(PlayNextCommandProperty); set => SetValue(PlayNextCommandProperty, value); }

    public static readonly DependencyProperty AddToQueueCommandProperty = DependencyProperty.Register(
        nameof(AddToQueueCommand), typeof(ICommand), typeof(MediaMenuFlyout), new PropertyMetadata(null));
    public ICommand AddToQueueCommand { get => (ICommand)GetValue(AddToQueueCommandProperty); set => SetValue(AddToQueueCommandProperty, value); }

    public static readonly DependencyProperty ContextItemProperty = DependencyProperty.Register(
        nameof(ContextItem), typeof(object), typeof(MediaMenuFlyout), new PropertyMetadata(null));
    public object ContextItem { get => GetValue(ContextItemProperty); set => SetValue(ContextItemProperty, value); }

    public static readonly DependencyProperty IsAdvancedModeEnabledProperty = DependencyProperty.Register(
        nameof(IsAdvancedModeEnabled), typeof(bool), typeof(MediaMenuFlyout), new PropertyMetadata(false));
    public bool IsAdvancedModeEnabled { get => (bool)GetValue(IsAdvancedModeEnabledProperty); set => SetValue(IsAdvancedModeEnabledProperty, value); }

    private MenuFlyoutItem? _playItem;
    private FontIcon? _playIcon;
    private MenuFlyoutItem? _playNextItem;
    private MenuFlyoutItem? _addToQueueItem;
    private MenuFlyoutSubItem? _addToPlaylistSubItem;
    private MenuFlyoutItem? _openWithItem;
    private MenuFlyoutItem? _openInFileExplorerItem;
    private MenuFlyoutItem? _propertiesItem;
    private MenuFlyoutItem? _playbackOptionsItem;
    private MenuFlyoutSeparator? _advancedSeparator;
    private readonly SetPlaybackOptionsCommand _playbackOptionsCommand = new();
    private bool _isInitialized;
    
    private readonly List<MenuFlyoutItemBase> _extraItems = new();

    public MediaMenuFlyout()
    {
        Opening += OnOpening;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _extraItems.AddRange(Items);
        Items.Clear();

        var symbolFont = (FontFamily)Application.Current.Resources["ScreenboxSymbolThemeFontFamily"];

        _playIcon = new FontIcon { FontFamily = symbolFont };
        _playItem = new MenuFlyoutItem { Icon = _playIcon };

        var playNextIcon = new FontIcon { FontFamily = symbolFont, Glyph = (string)Application.Current.Resources["PlayAddGlyph"] };
        _playNextItem = new MenuFlyoutItem { Icon = playNextIcon, Text = Resources.PlayNext };

        string addToQueueGlyph = GlobalizationHelper.IsRightToLeftLanguage 
            ? (string)Application.Current.Resources["PlaylistMusicAddMirroredGlyph"] 
            : (string)Application.Current.Resources["PlaylistMusicAddGlyph"];
        var addToQueueIcon = new FontIcon { FontFamily = symbolFont, Glyph = addToQueueGlyph };
        _addToQueueItem = new MenuFlyoutItem { Icon = addToQueueIcon, Text = Resources.AddToQueue };

        string addToPlaylistGlyph = GlobalizationHelper.IsRightToLeftLanguage 
            ? (string)Application.Current.Resources["PlaylistAddMirroredGlyph"] 
            : (string)Application.Current.Resources["PlaylistAddGlyph"];
        var addToPlaylistIcon = new FontIcon { FontFamily = symbolFont, Glyph = addToPlaylistGlyph };
        _addToPlaylistSubItem = new MenuFlyoutSubItem { Text = Resources.AddToPlaylist, Icon = addToPlaylistIcon };
        
        Interaction.GetBehaviors(this).Add(new Screenbox.Behaviors.AddToPlaylistFlyoutBehavior { TargetSubItem = _addToPlaylistSubItem });

        var openWithIcon = new FontIcon { Glyph = "\uE7AC", MirroredWhenRightToLeft = true };
        _openWithItem = new MenuFlyoutItem { Icon = openWithIcon, Text = Resources.OpenWith, Command = (ICommand)Application.Current.Resources["OpenWithCommand"] };

        var openInFileExplorerIcon = new FontIcon { Glyph = "\uEC50" };
        _openInFileExplorerItem = new MenuFlyoutItem
        {
            Icon = openInFileExplorerIcon,
            Text = Resources.OpenInFileExplorer,
            Command = (ICommand)Application.Current.Resources["OpenInFileExplorerCommand"],
            Visibility = DeviceInfoHelper.IsDesktop ? Visibility.Visible : Visibility.Collapsed
        };

        var propertiesIcon = new FontIcon { Glyph = "\uE946" };
        _propertiesItem = new MenuFlyoutItem { Icon = propertiesIcon, Text = Resources.Properties, Command = (ICommand)Application.Current.Resources["ShowPropertiesCommand"] };

        var settingsIcon = new SymbolIcon { Symbol = Symbol.Setting };
        _playbackOptionsItem = new MenuFlyoutItem
        {
            Icon = settingsIcon,
            Text = Resources.SetPlaybackOptions,
            Command = _playbackOptionsCommand
        };
        _advancedSeparator = new MenuFlyoutSeparator();

        Items.Add(_playItem);
        Items.Add(_playNextItem);
        Items.Add(_addToQueueItem);
        Items.Add(new MenuFlyoutSeparator());
        Items.Add(_addToPlaylistSubItem);
        Items.Add(_openWithItem);
        Items.Add(_openInFileExplorerItem);
        Items.Add(_propertiesItem);

        foreach (var extraItem in _extraItems)
        {
            Items.Add(extraItem);
        }

        Items.Add(_advancedSeparator);
        Items.Add(_playbackOptionsItem);
    }

    private void OnOpening(object? sender, object e)
    {
        EnsureInitialized();

        var mediaVm = ContextItem as MediaViewModel;
        if (ContextItem is StorageItemViewModel storageItem)
        {
            mediaVm = storageItem.Media;
        }

        _playItem!.Command = PlayCommand;
        _playItem!.CommandParameter = ContextItem;

        _playNextItem!.Command = PlayNextCommand;
        _playNextItem!.CommandParameter = ContextItem;

        _addToQueueItem!.Command = AddToQueueCommand;
        _addToQueueItem!.CommandParameter = ContextItem;

        _openWithItem!.CommandParameter = mediaVm;
        _openInFileExplorerItem!.CommandParameter = mediaVm;
        _propertiesItem!.CommandParameter = mediaVm;

        if (mediaVm != null)
        {
            _playItem!.Text = ItemLabelHelper.GetPlayPauseLabel(mediaVm.IsPlaying);
            _playIcon!.Glyph = GlyphConverter.ToPlayPauseGlyph(mediaVm.IsPlaying);
        }
        else
        {
            _playItem!.Text = ItemLabelHelper.GetPlayPauseLabel(false);
            _playIcon!.Glyph = GlyphConverter.ToPlayPauseGlyph(false);
        }

        _advancedSeparator!.Visibility = IsAdvancedModeEnabled ? Visibility.Visible : Visibility.Collapsed;
        _playbackOptionsItem!.Visibility = IsAdvancedModeEnabled ? Visibility.Visible : Visibility.Collapsed;
        
        _playbackOptionsItem!.CommandParameter = ContextItem;
        _playbackOptionsCommand.PlayCommand = PlayCommand;
    }
}
