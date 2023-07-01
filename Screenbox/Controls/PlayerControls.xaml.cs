#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.ViewModels;
using System;
using System.Linq;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

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
            nameof(BackgroundTransition),
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

        internal CommonViewModel Common { get; }

        private Flyout? _castFlyout;

        public PlayerControls()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerControlsViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            PlayerContextMenu = NormalPlayerContextMenu;
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

        private string GetRepeatModeGlyph(MediaPlaybackAutoRepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case MediaPlaybackAutoRepeatMode.None:
                    return "\uf5e7";
                case MediaPlaybackAutoRepeatMode.List:
                    return "\ue8ee";
                case MediaPlaybackAutoRepeatMode.Track:
                    return "\ue8ed";
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }
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

        private static bool IsValueEqualTag(double value, object? tag)
        {
            if (!double.TryParse(tag as string, out double tagValue)) return false;
            return Math.Abs(value - tagValue) < 0.0001;
        }
    }
}
