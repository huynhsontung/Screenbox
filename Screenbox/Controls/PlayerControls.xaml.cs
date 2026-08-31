using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using WinRT;

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

    private Flyout? _castFlyout;

    public PlayerControls()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayerControlsViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        AudioTrackSubtitlePicker.ShowSubtitleOptionsCommand = new RelayCommand(ShowSubtitleOptions);
        AudioTrackSubtitlePicker.ShowAudioOptionsCommand = new RelayCommand(ShowAudioOptions);
    }

    [DynamicWindowsRuntimeCast(typeof(Flyout))]
    private void ShowSubtitleOptions()
    {
        AudioSubtitlePickerFlyout.Hide();
        Flyout flyout = (Flyout)Resources["SubtitleOptionsFlyout"];
        flyout.ShowAt(AudioAndCaptionButton);
    }

    [DynamicWindowsRuntimeCast(typeof(Flyout))]
    private void ShowAudioOptions()
    {
        AudioSubtitlePickerFlyout.Hide();
        Flyout flyout = (Flyout)Resources["AudioOptionsFlyout"];
        flyout.ShowAt(AudioAndCaptionButton);
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
        if (SpeedSlider.Value != ViewModel.PlaybackRate)
        {
            SpeedSlider.Value = ViewModel.PlaybackRate;
        }
        else
        {
            SelectAlternatePlaybackSpeedItem(ViewModel.PlaybackRate);
        }
    }

    [DynamicWindowsRuntimeCast(typeof(Flyout))]
    private void CustomAspectRatioMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        Flyout customAspectFlyout = (Flyout)Resources["CustomAspectRatioFlyout"];
        customAspectFlyout.ShowAt(MoreButton);
    }

    private void SpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        double newValue = Math.Max(e.NewValue, 0.05);
        if (Math.Abs(SpeedSlider.Value - newValue) > 0.0001)
        {
            SpeedSlider.Value = newValue;
        }

        ViewModel.SetPlaybackRateCommand.Execute(newValue);
        SelectAlternatePlaybackSpeedItem(newValue);
    }

    [DynamicWindowsRuntimeCast(typeof(RadioMenuFlyoutItem))]
    private void SelectAlternatePlaybackSpeedItem(double playbackSpeed)
    {
        bool isMenuValue = (int)(playbackSpeed * 100) % 25 == 0;
        if (isMenuValue &&
            PlaybackSpeedSubMenu.Items?.FirstOrDefault(x =>
                    x.Tag is double predefinedSpeed && Math.Abs(predefinedSpeed - playbackSpeed) < 0.0001) is
                RadioMenuFlyoutItem matchItem)
        {
            matchItem.IsChecked = true;
        }
        else
        {
            CustomPlaybackSpeedMenuItem.IsChecked = true;
        }
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
        // Ignore the play/pause shortcut when the spacebar is pressed in mini-player visual state.
        if (args.KeyboardAccelerator.Key == VirtualKey.Space && ViewModel.IsMinimal) return;

        // Override default keyboard accelerator to show badge.
        args.Handled = true;
        ViewModel.PlayPauseWithBadge();
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
}
