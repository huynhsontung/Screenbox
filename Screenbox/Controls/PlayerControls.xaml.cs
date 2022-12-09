#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlayerControls : UserControl
    {
        public static readonly DependencyProperty IsMinimalProperty = DependencyProperty.Register(
            nameof(IsMinimal),
            typeof(bool),
            typeof(PlayerControls),
            new PropertyMetadata(false));

        public static readonly DependencyProperty PlayerContextMenuProperty = DependencyProperty.Register(
            nameof(PlayerContextMenu),
            typeof(MenuFlyout),
            typeof(PlayerControls),
            new PropertyMetadata(default(MenuFlyout)));

        public static readonly DependencyProperty BackgroundTransitionProperty = DependencyProperty.Register(
            "BackgroundTransition",
            typeof(BrushTransition),
            typeof(PlayerControls),
            new PropertyMetadata(null));

        public BrushTransition BackgroundTransition
        {
            get => (BrushTransition)GetValue(BackgroundTransitionProperty);
            set => SetValue(BackgroundTransitionProperty, value);
        }

        public MenuFlyout PlayerContextMenu
        {
            get => (MenuFlyout)GetValue(PlayerContextMenuProperty);
            private set => SetValue(PlayerContextMenuProperty, value);
        }

        public bool IsMinimal
        {
            get => (bool)GetValue(IsMinimalProperty);
            set => SetValue(IsMinimalProperty, value);
        }

        internal PlayerControlsViewModel ViewModel => (PlayerControlsViewModel)DataContext;

        private Flyout? _castFlyout;

        public PlayerControls()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlayerControlsViewModel>();
            PlayerContextMenu = NormalPlayerContextMenu;
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

        private void SpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ViewModel.PlaybackSpeed = e.NewValue;
            SelectAlternatePlaybackSpeedItem(e.NewValue);
        }

        private void SelectAlternatePlaybackSpeedItem(double playbackSpeed)
        {
            bool isMenuValue = (int)(playbackSpeed * 100) % 25 == 0;
            if (isMenuValue && PlaybackSpeedSubMenu.Items?.FirstOrDefault(x => IsValueEqualTag(playbackSpeed, x.Tag)) is RadioMenuFlyoutItem matchItem)
            {
                matchItem.IsChecked = true;
            }
            else
            {
                CustomPlaybackSpeedMenuItem.IsChecked = true;
            }
        }

        private static bool IsValueEqualTag(double value, object? tag)
        {
            if (!double.TryParse(tag as string, out double tagValue)) return false;
            return Math.Abs(value - tagValue) < 0.0001;
        }
    }
}
