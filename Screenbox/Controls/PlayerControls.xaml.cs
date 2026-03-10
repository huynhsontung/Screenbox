#nullable enable

using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
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
        get => (BrushTransition)GetValue(BackgroundTransitionProperty);
        set => SetValue(BackgroundTransitionProperty, value);
    }

    public MenuFlyout? PlayerContextMenu => (MenuFlyout?)MoreButton.Flyout;

    internal PlayerControlsViewModel ViewModel => (PlayerControlsViewModel)DataContext;

    internal CommonViewModel Common { get; }

    /// <summary>
    /// Wraps <see cref="PlayerControlsViewModel.SaveSnapshotCommand"/> with a
    /// <see cref="NotificationCommand"/> that sends a localized error notification on failure.
    /// </summary>
    public ICommand SaveSnapshotCommand { get; }

    /// <summary>
    /// Wraps <see cref="CommonViewModel.OpenFilesCommand"/> with a
    /// <see cref="NotificationCommand"/> that sends a localized error notification on failure.
    /// </summary>
    public ICommand OpenFilesCommand { get; }

    private Flyout? _castFlyout;

    public PlayerControls()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayerControlsViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        AudioTrackSubtitlePicker.ShowSubtitleOptionsCommand = new RelayCommand(ShowSubtitleOptions);
        AudioTrackSubtitlePicker.ShowAudioOptionsCommand = new RelayCommand(ShowAudioOptions);

        SaveSnapshotCommand = new NotificationCommand(
            ViewModel.SaveSnapshotCommand,
            onFailure: e => ViewModel.SendErrorMessage(Screenbox.Strings.Resources.FailedToSaveFrameNotificationTitle, e.Message));

        OpenFilesCommand = new NotificationCommand(
            Common.OpenFilesCommand,
            onFailure: e => Common.SendErrorMessage(Screenbox.Strings.Resources.FailedToOpenFilesNotificationTitle, e.Message));
    }    private void ShowSubtitleOptions()
    {
        AudioSubtitlePickerFlyout.Hide();
        Flyout flyout = (Flyout)Resources["SubtitleOptionsFlyout"];
        flyout.ShowAt(AudioAndCaptionButton);
    }

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
        _castFlyout.ShowAt(MoreButton, new FlyoutShowOptions { Placement = FlyoutPlacementMode.TopEdgeAlignedRight });
    }

    private void CustomSpeedMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        Flyout customSpeedFlyout = (Flyout)Resources["CustomPlaybackSpeedFlyout"];
        customSpeedFlyout.ShowAt(MoreButton);
        if (SpeedSlider.Value != ViewModel.PlaybackSpeed)
        {
            SpeedSlider.Value = ViewModel.PlaybackSpeed;
        }
        else
        {
            SelectAlternatePlaybackSpeedItem(ViewModel.PlaybackSpeed);
        }
    }

    private void CustomAspectRatioMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        Flyout customAspectFlyout = (Flyout)Resources["CustomAspectRatioFlyout"];
        customAspectFlyout.ShowAt(MoreButton);
    }

    private void SpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        ViewModel.PlaybackSpeed = e.NewValue;
        SelectAlternatePlaybackSpeedItem(e.NewValue);
    }

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
        var result = ViewModel.ProcessToggleSubtitleKeyDown(args.KeyboardAccelerator.Modifiers);
        args.Handled = result.Handled;
        if (result.Handled)
        {
            string label = !string.IsNullOrEmpty(result.TrackLabel)
                ? result.TrackLabel!
                : Screenbox.Strings.Resources.None;
            ViewModel.SendStatusMessage(Screenbox.Strings.Resources.SubtitleStatus(label));
        }
    }
}

