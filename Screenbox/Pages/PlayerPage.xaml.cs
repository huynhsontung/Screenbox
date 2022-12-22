#nullable enable

using System;
using System.ComponentModel;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Diagnostics;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Controls;
using Screenbox.Services;
using Screenbox.ViewModels;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using Windows.System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        internal PlayerPageViewModel ViewModel => (PlayerPageViewModel)DataContext;

        private const VirtualKey PlusKey = (VirtualKey)0xBB;
        private const VirtualKey MinusKey = (VirtualKey)0xBD;
        private const VirtualKey AddKey = (VirtualKey)0x6B;
        private const VirtualKey SubtractKey = (VirtualKey)0x6D;

        private CancellationTokenSource? _animationCancellationTokenSource;

        public PlayerPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlayerPageViewModel>();
            RegisterSeekBarPointerHandlers();
            UpdatePreviewType();

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            AlbumArtImage.RegisterPropertyChangedCallback(Image.SourceProperty, AlbumArtImageOnSourceChanged);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is true)
            {
                LayoutRoot.Transitions.Clear();
                ViewModel.PlayerVisible = true;
            }
        }

        private void FocusVideoView()
        {
            VideoView.Focus(FocusState.Programmatic);
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
            UpdateSystemCaptionButtonForeground();
        }

        private void AlbumArtImageOnSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            PlayBackgroundArtChangeCrossFadeAnimation();
        }

        private void OnLoading(FrameworkElement sender, object args)
        {
            if (!ViewModel.PlayerVisible)
                VisualStateManager.GoToState(this, "MiniPlayer", false);
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (LayoutRoot.Transitions.Count != 0) return;
            PaneThemeTransition transition = new()
            {
                Edge = EdgeTransitionLocation.Bottom
            };
                
            LayoutRoot.Transitions.Add(transition);
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void BackgroundElementOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double edgeLength = Math.Max(e.NewSize.Width, e.NewSize.Height);
            BackgroundArt.Width = edgeLength;
            BackgroundArt.Height = edgeLength;
        }

        private void RegisterSeekBarPointerHandlers()
        {
            SeekBar? seekBar = PlayerControls.FindDescendant<SeekBar>();
            Guard.IsNotNull(seekBar, nameof(seekBar));
            seekBar.AddHandler(PointerPressedEvent, (PointerEventHandler)SeekBarPointerPressedEventHandler, true);
            seekBar.AddHandler(PointerReleasedEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
            seekBar.AddHandler(PointerCanceledEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
        }

        private void SeekBarPointerPressedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerPressed = true;
        }

        private void SeekBarPointerReleasedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerPressed = false;
            FocusVideoView();
        }

        private void OnLayoutVisualStateChanged(object _, VisualStateChangedEventArgs args)
        {
            if (args.OldState?.Name == "MiniPlayer" && (args.NewState == null || args.NewState.Name == "Normal"))
                FocusVideoView();
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerPageViewModel.ControlsHidden):
                    VisualStateManager.GoToState(this, ViewModel.ControlsHidden ? "ControlsHidden" : "ControlsVisible", true);
                    break;
                case nameof(PlayerPageViewModel.ViewMode):
                    switch (ViewModel.ViewMode)
                    {
                        case WindowViewMode.Default:
                            VisualStateManager.GoToState(this, "Normal", true);
                            break;
                        case WindowViewMode.Compact:
                            ViewModel.PlayerVisible = true;
                            VisualStateManager.GoToState(this, "CompactOverlay", true);
                            SetTitleBar();
                            break;
                        case WindowViewMode.FullScreen:
                            ViewModel.PlayerVisible = true;
                            VisualStateManager.GoToState(this, "Fullscreen", true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case nameof(PlayerPageViewModel.AudioOnly):
                    VisualStateManager.GoToState(this, ViewModel.AudioOnly ?? false ? "AudioOnly" : "Video", true);
                    UpdateSystemCaptionButtonForeground();
                    UpdatePreviewType();
                    break;
                case nameof(PlayerPageViewModel.PlayerVisible):
                    if (ViewModel.PlayerVisible)
                    {
                        VisualStateManager.GoToState(this, "NoPreview", true);
                        VisualStateManager.GoToState(this, "Normal", true);
                        SetTitleBar();
                    }
                    else
                    {
                        VisualStateManager.GoToState(this, "MiniPlayer", true);
                    }

                    UpdatePreviewType();
                    UpdateMiniPlayerMargin();
                    break;
                case nameof(PlayerPageViewModel.NavigationViewDisplayMode) when ViewModel.ViewMode == WindowViewMode.Default:
                    UpdateMiniPlayerMargin();
                    break;
            }
        }

        private async void PlayBackgroundArtChangeCrossFadeAnimation()
        {
            // AnimationSet does not throw exception on cancellation
            _animationCancellationTokenSource?.Cancel();
            if (BackgroundElement.Visibility == Visibility.Collapsed ||
            BackgroundArt.Visibility == Visibility.Collapsed)
            {
                BackgroundImage.Source = AlbumArtImage.Source;
                return;
            }

            using CancellationTokenSource cts = _animationCancellationTokenSource = new CancellationTokenSource();
            if (ViewModel.Media == null)
            {
                await BackgroundArtFadeOutAnimation.StartAsync(cts.Token);
                BackgroundImage.Source = null;
            }
            else if (BackgroundImage.Source == null)
            {
                BackgroundImageNext.Visibility = Visibility.Collapsed;
                BackgroundImage.GetVisual().Opacity = 0;
                BackgroundImage.Source = AlbumArtImage.Source;
                await BackgroundArtFadeInAnimation.StartAsync(cts.Token);
            }
            else
            {
                BackgroundImageNext.Visibility = Visibility.Visible;
                await BackgroundArtFadeOutAnimation.StartAsync(cts.Token);
                BackgroundImage.Source = AlbumArtImage.Source;
                await BackgroundArtFadeInAnimation.StartAsync(cts.Token);
                BackgroundImageNext.Visibility = Visibility.Collapsed;
            }

            if (cts == _animationCancellationTokenSource)
                _animationCancellationTokenSource = null;
        }

        private void UpdateSystemCaptionButtonForeground()
        {
            if (ApplicationView.GetForCurrentView()?.TitleBar is { } titleBar)
            {
                titleBar.ButtonForegroundColor = ViewModel.AudioOnly ?? false ? null : Colors.White;
            }
        }

        private void UpdatePreviewType()
        {
            if (ViewModel.PlayerVisible || ViewModel.ViewMode == WindowViewMode.Compact)
            {
                VisualStateManager.GoToState(this, "NoPreview", true);
            }
            else
            {
                VisualStateManager.GoToState(this, ViewModel.AudioOnly ?? false ? "AudioPreview" : "VideoPreview", true);
            }
        }

        private void UpdateMiniPlayerMargin()
        {
            if (ViewModel.PlayerVisible || ViewModel.ViewMode == WindowViewMode.Compact)
            {
                VisualStateManager.GoToState(this, "NoMargin", false);
            }
            else
            {
                switch (ViewModel.NavigationViewDisplayMode)
                {
                    case NavigationViewDisplayMode.Minimal:
                        VisualStateManager.GoToState(this, "MinimalMargin", false);
                        break;
                    case NavigationViewDisplayMode.Compact:
                        VisualStateManager.GoToState(this, "CompactMargin", false);
                        break;
                    case NavigationViewDisplayMode.Expanded:
                        VisualStateManager.GoToState(this, "ExpandedMargin", false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void PlayQueueButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PlayQueueFlyout == null)
            {
                FindName(nameof(PlayQueueFlyout));
            }
        }
    }
}
