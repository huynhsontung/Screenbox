#nullable enable

using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

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

        /// <summary>
        /// Gets the repeat mode glyph code based on the current auto-repeat mode of the player.
        /// </summary>
        /// <remarks>The glyph code adapts according to the current text reading order.</remarks>
        /// <param name="repeatMode">A value of the enumeration that specifies the auto repeat mode for the <see cref="MediaPlaybackAutoRepeatMode"/>.</param>
        /// <returns>
        /// <strong>Repeat Off</strong> glyph code <see cref="string"/> if <paramref name="repeatMode"/> is <see cref="MediaPlaybackAutoRepeatMode.None"/>,
        /// <strong>Repeat All</strong> glyph code for <see cref="MediaPlaybackAutoRepeatMode.List"/>, or <strong>Repeat One</strong> glyph code for <see cref="MediaPlaybackAutoRepeatMode.Track"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="repeatMode"/> is not one of the valid <see cref="MediaPlaybackAutoRepeatMode"/> value.</exception>
        private string GetRepeatModeGlyph(MediaPlaybackAutoRepeatMode repeatMode)
        {
            return repeatMode switch
            {
                MediaPlaybackAutoRepeatMode.None => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F0127" : "\uF5E7",
                MediaPlaybackAutoRepeatMode.List => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F004E" : "\uE8EE",
                MediaPlaybackAutoRepeatMode.Track => GlobalizationHelper.IsRightToLeftLanguage ? "\U000F004D" : "\uE8ED",
                _ => throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null),
            };
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

        /// <summary>
        /// Get the Playback Speed glyph for a speed range to set on <see cref="PlaybackSpeedSubMenu"/>.
        /// </summary>
        /// <returns>Speed Medium glyph if PlaybackSpeed equals 1 x, Speed High glyph if its greater than 1 x, Auto Racing glyph if its greater than 1.75 x, Speed Low glyph if its less than 1 x, Speed Off glyph if its less than 0.25 x</returns>
        private string GetPlaybackSpeedGlyph(double playbackSpeed)
        {
            return playbackSpeed switch
            {
                >= 1.75 => "\uEB24",
                > 1.01 => "\uEC4A",
                <= 0.25 => "\uEC48",
                < 0.99 => "\U000F00A4",
                _ => "\uEC49"
            };
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
    }
}
