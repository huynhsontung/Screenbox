#nullable enable

using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.ViewModels;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
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

        private Flyout? _castFlyout;

        public PlayerControls()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerControlsViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            AudioTrackSubtitlePicker.ShowSubtitleOptionsCommand = new RelayCommand(ShowSubtitleOptions);
            AudioTrackSubtitlePicker.ShowAudioOptionsCommand = new RelayCommand(ShowAudioOptions);
        }

        private void ShowSubtitleOptions()
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
            args.Handled = ViewModel.ProcessSubtitleToggle(args.KeyboardAccelerator.Modifiers);
        }
    }
}
