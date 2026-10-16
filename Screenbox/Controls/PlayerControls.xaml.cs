using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Screenbox.UI;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class PlayerControls : UserControl
{
    public static readonly DependencyProperty BackgroundTransitionProperty = DependencyProperty.Register(
        nameof(BackgroundTransition),
        typeof(BrushTransition),
        typeof(PlayerControls),
        new PropertyMetadata(null));

    public BrushTransition BackgroundTransition
    {
        [DynamicWindowsRuntimeCast(typeof(BrushTransition))]
        get => (BrushTransition)GetValue(BackgroundTransitionProperty);
        set => SetValue(BackgroundTransitionProperty, value);
    }

    public MenuFlyout? PlayerContextMenu
    {
        [DynamicWindowsRuntimeCast(typeof(MenuFlyout))]
        get => (MenuFlyout?)MoreButton.Flyout;
    }

    internal PlayerControlsViewModel ViewModel => (PlayerControlsViewModel)DataContext;

    internal CommonViewModel Common { get; }

    internal PlaybackSessionViewModel PlaybackSession { get; }

    private static readonly double[] _playbackRates =
    [
        PlaybackSpeedControl.Speed025, PlaybackSpeedControl.Speed050, PlaybackSpeedControl.Speed075,
        PlaybackSpeedControl.Speed100, PlaybackSpeedControl.Speed125, PlaybackSpeedControl.Speed150, PlaybackSpeedControl.Speed175,
        PlaybackSpeedControl.Speed200, PlaybackSpeedControl.Speed400,
    ];

    private Flyout? _castFlyout;

    public PlayerControls()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayerControlsViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        PlaybackSession = Ioc.Default.GetRequiredService<PlaybackSessionViewModel>();
    }

    public void FocusFirstButton(FocusState value = FocusState.Programmatic)
    {
        PlayPauseButton.Focus(value);
    }

    private void CastMenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        _castFlyout ??= CastControl.GetFlyout();
        _castFlyout.ShowAt(MoreButton, new FlyoutShowOptions { Placement = GlobalizationHelper.MirrorWhenRightToLeft(FlyoutPlacementMode.TopEdgeAlignedRight) });
    }

    [DynamicWindowsRuntimeCast(typeof(Flyout))]
    private void CustomSpeedMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        Flyout customSpeedFlyout = (Flyout)Resources["CustomPlaybackSpeedFlyout"];
        customSpeedFlyout.ShowAt(MoreButton);
    }

    [DynamicWindowsRuntimeCast(typeof(Flyout))]
    private void CustomAspectRatioMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        Flyout customAspectFlyout = (Flyout)Resources["CustomAspectRatioFlyout"];
        AspectRatioTextBox.Header = Strings.Resources.CustomAspectRatio;
        customAspectFlyout.ShowAt(MoreButton);
    }

    private bool IsCastButtonEnabled(bool hasActiveItem)
    {
        if (_castFlyout?.Content is CastControl control)
        {
            return control.ViewModel.IsCasting || hasActiveItem;
        }

        return hasActiveItem;
    }

    [DynamicWindowsRuntimeCast(typeof(RadioMenuFlyoutItem))]
    private void AspectRatioTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        string aspectRatio = AspectRatioTextBox.Text;
        if (!aspectRatio.Contains(':')) return;
        if (AspectRatioSubMenu.Items?.FirstOrDefault(x => (string)x.Tag == aspectRatio) is RadioMenuFlyoutItem
            matchItem)
        {
            matchItem.IsChecked = true;
            matchItem.Command?.Execute(matchItem.CommandParameter);
        }
        else
        {
            CustomAspectRatioMenuItem.IsChecked = true;
            ViewModel.SetAspectRatioCommand.Execute(aspectRatio);
        }
    }

    private void PlayPauseKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.HandlePlaybackStateToggleKey();
        args.Handled = true;
    }

    [DynamicWindowsRuntimeCast(typeof(SelectorItem))]
    private void PreviousNextKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (FocusManager.GetFocusedElement() is SelectorItem && args.KeyboardAccelerator.Key is VirtualKey.PageUp or VirtualKey.PageDown)
        {
            args.Handled = true;
            return;
        }

        ViewModel.HandleTrackNavigationKey(args.KeyboardAccelerator.Key, args.KeyboardAccelerator.Modifiers);
        args.Handled = true;
    }

    private void ToggleSubtitleKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        // Ignore subtitle toggle when the key is pressed without modifiers and subtitles cannot be uniquely selected.
        if (args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.None && !ViewModel.HasSingleSubtitleTrackCount)
            return;

        ViewModel.HandleSubtitleToggleKey(args.KeyboardAccelerator.Modifiers);
        args.Handled = true;
    }

    private Visibility GetChapterVisibility(bool isEnabled, int count)
    {
        return isEnabled && count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool IsCustomPlaybackRateSelected(double currentRate)
    {
        foreach (double rate in _playbackRates)
        {
            if (DoubleHelper.AreClose(currentRate, rate))
                return false;
        }

        return true;
    }
}
