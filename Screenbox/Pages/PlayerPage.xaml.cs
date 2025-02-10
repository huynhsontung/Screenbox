#nullable enable

using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Screenbox.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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
        private const VirtualKey PeriodKey = (VirtualKey)190;
        private const VirtualKey CommaKey = (VirtualKey)188;

        private readonly DispatcherQueueTimer _delayFlyoutOpenTimer;
        private CancellationTokenSource? _animationCancellationTokenSource;
        private bool _startup;

        public PlayerPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerPageViewModel>();
            _delayFlyoutOpenTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();

            RegisterSeekBarPointerHandlers();
            UpdatePreviewType();

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            AlbumArtImage.RegisterPropertyChangedCallback(ImageBrush.ImageSourceProperty, AlbumArtImageOnSourceChanged);
            LayoutRoot.ActualThemeChanged += OnActualThemeChanged;

            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();   // For navigation events
            navigationService.Navigated += NavigationServiceOnNavigated;
        }

        private void NavigationServiceOnNavigated(object sender, EventArgs e)
        {
            if (ViewModel.PlayerVisibility != PlayerVisibilityState.Visible) return;
            ViewModel.GoBack();
            if (PlayQueueFlyout.IsOpen)
                PlayQueueFlyout.Hide();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UpdateBackgroundAcrylicOpacity(ActualTheme);

            // DO NOT SET CONTENT VISUAL STATE HERE
            // It will cause element theme to not propagate correctly
            // VisualStateManager.GoToState(this, "Video", false);

            if (e.Parameter is true)
            {
                LayoutRoot.Transitions.Clear();
                ViewModel.PlayerVisibility = PlayerVisibilityState.Visible;
                ViewModel.OnFileLaunched();
                _startup = true;
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);
            if (ViewModel.PlayerVisibility != PlayerVisibilityState.Visible) return;
            bool handled = true;
            bool shouldHideControls = ViewModel is { ControlsHidden: false, ViewMode: WindowViewMode.Default };
            switch (e.Key)
            {
                case VirtualKey.GamepadY when ViewModel.ViewMode != WindowViewMode.Compact:
                    ViewModel.ControlsHidden = false;
                    PlayQueueFlyout.ShowAt(TitleBarArea,
                        new FlyoutShowOptions { Placement = FlyoutPlacementMode.Bottom });
                    break;
                case VirtualKey.GamepadMenu:
                    VideoView.ContextFlyout.ShowAt(VideoView,
                        new FlyoutShowOptions { Placement = FlyoutPlacementMode.Auto });
                    break;
                case VirtualKey.Escape when shouldHideControls:
                case VirtualKey.GamepadB when shouldHideControls:
                    handled = ViewModel.TryHideControls();
                    break;
                default:
                    return;
            }

            e.Handled = handled;
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarDragRegion);
            UpdateSystemCaptionButtonForeground();
        }

        private void AlbumArtImageOnSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            PlayBackgroundArtChangeCrossFadeAnimation();
        }

        private void OnLoading(FrameworkElement sender, object args)
        {
            if (ViewModel.PlayerVisibility == PlayerVisibilityState.Hidden)
                VisualStateManager.GoToState(this, "Hidden", false);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (LayoutRoot.Transitions.Count != 0) return;
            PaneThemeTransition transition = new()
            {
                Edge = EdgeTransitionLocation.Bottom
            };

            LayoutRoot.Transitions.Add(transition);

            if (ViewModel.PlayerVisibility == PlayerVisibilityState.Visible)
            {
                // Focus can fail if player is file activated
                // Controls are disabled by default until playback is ready
                PlayerControls.FocusFirstButton();
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            // In RTL languages, Grid is flipped automatically.
            // Left is always the side without the system controls.
            // Left padding should only be set if we pin flow direction on the title bar.
            // LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(Math.Max(sender.SystemOverlayLeftInset, sender.SystemOverlayRightInset));
        }

        private void BackgroundElementOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double overshoot = 200;   // extra space for animation
            double edgeLength = Math.Max(e.NewSize.Width, e.NewSize.Height) + overshoot;
            BackgroundArt.Width = edgeLength;
            BackgroundArt.Height = edgeLength;
        }

        private void RegisterSeekBarPointerHandlers()
        {
            SeekBar? seekBar = PlayerControls.FindDescendant<SeekBar>();
            Guard.IsNotNull(seekBar, nameof(seekBar));
            seekBar.AddHandler(PointerPressedEvent, (PointerEventHandler)SeekBarPointerPressedOrEnteredEventHandler, true);
            seekBar.AddHandler(PointerReleasedEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
            seekBar.AddHandler(PointerCanceledEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
            seekBar.AddHandler(PointerEnteredEvent, (PointerEventHandler)SeekBarPointerPressedOrEnteredEventHandler, false);
            seekBar.AddHandler(PointerExitedEvent, (PointerEventHandler)SeekBarPointerExitedEventHandler, false);
        }

        private void SeekBarPointerPressedOrEnteredEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerInteracting = true;
        }

        private void SeekBarPointerReleasedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerInteracting = false;
            if (ViewModel.PlayerVisibility == PlayerVisibilityState.Visible)
                PlayerControls.FocusFirstButton();
        }

        private void SeekBarPointerExitedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerInteracting = false;
        }

        private void OnLayoutVisualStateChanged(object _, VisualStateChangedEventArgs args)
        {
            bool expanding = args.OldState?.Name == nameof(MiniPlayer) || args.OldState?.Name == nameof(Hidden) &&
                (args.NewState == null || args.NewState.Name == nameof(Normal));

            bool collapsing = args.OldState?.Name == nameof(Normal) && args.NewState?.Name == nameof(MiniPlayer);

            if (expanding || collapsing) PlayerControls.FocusFirstButton();
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerPageViewModel.ControlsHidden):
                    VisualStateManager.GoToState(this, ViewModel.ControlsHidden ? "ControlsHidden" : "ControlsVisible", true);
                    if (!ViewModel.ControlsHidden)
                    {
                        PlayerControls.FocusFirstButton();
                    }

                    break;
                case nameof(PlayerPageViewModel.ViewMode):
                    switch (ViewModel.ViewMode)
                    {
                        case WindowViewMode.Default:
                            VisualStateManager.GoToState(this, "Normal", true);
                            break;
                        case WindowViewMode.Compact:
                            ViewModel.PlayerVisibility = PlayerVisibilityState.Visible;
                            VisualStateManager.GoToState(this, "CompactOverlay", true);
                            SetTitleBar();
                            break;
                        case WindowViewMode.FullScreen:
                            ViewModel.PlayerVisibility = PlayerVisibilityState.Visible;
                            VisualStateManager.GoToState(this, "Fullscreen", true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    UpdateContentState();
                    break;
                case nameof(PlayerPageViewModel.AudioOnly):
                    UpdateContentState();
                    UpdateSystemCaptionButtonForeground();
                    UpdatePreviewType();
                    break;
                case nameof(PlayerPageViewModel.PlayerVisibility):
                    switch (ViewModel.PlayerVisibility)
                    {
                        case PlayerVisibilityState.Visible:
                            VisualStateManager.GoToState(this, "NoPreview", true);
                            VisualStateManager.GoToState(this, "Normal", true);
                            SetTitleBar();
                            break;
                        case PlayerVisibilityState.Minimal:
                            VisualStateManager.GoToState(this, "MiniPlayer", true);
                            break;
                        case PlayerVisibilityState.Hidden:
                            VisualStateManager.GoToState(this, "Hidden", true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    UpdateContentState();
                    UpdatePreviewType();
                    UpdateMiniPlayerMargin();
                    break;
                case nameof(PlayerPageViewModel.NavigationViewDisplayMode) when ViewModel.ViewMode == WindowViewMode.Default:
                    UpdateMiniPlayerMargin();
                    break;
                case nameof(PlayerPageViewModel.IsPlaying):
                    if (ViewModel.IsPlaying)
                    {
                        if (_startup)
                        {
                            // Only when the app is file activated
                            // Wait till playback starts then focus the player controls
                            _startup = false;
                            PlayerControls.FocusFirstButton();
                        }

                        BackgroundArtAnimation.Resume();
                    }
                    else
                    {
                        BackgroundArtAnimation.Pause();
                    }

                    break;
                case nameof(PlayerPageViewModel.ShowVisualizer):
                    if (ViewModel.ShowVisualizer)
                    {
                        // Load the visualizer if not already
                        FindName(nameof(LivelyWallpaperPlayer));
                    }

                    UpdateContentState();
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
                BackgroundImage.Source = AlbumArtImage.ImageSource;
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
                BackgroundImage.Source = AlbumArtImage.ImageSource;
                await BackgroundArtFadeInAnimation.StartAsync(cts.Token);
            }
            else
            {
                BackgroundImageNext.Visibility = Visibility.Visible;
                await BackgroundArtFadeOutAnimation.StartAsync(cts.Token);
                BackgroundImage.Source = AlbumArtImage.ImageSource;
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
                titleBar.ButtonForegroundColor = ViewModel.AudioOnly ? null : Colors.White;
            }
        }

        private void UpdateContentState()
        {
            var contentVisualStateName = ViewModel.AudioOnly
                ? (ViewModel is { ShowVisualizer: true, PlayerVisibility: PlayerVisibilityState.Visible, ViewMode: not WindowViewMode.Compact }
                    ? "AudioWithVisualizer"
                    : "AudioOnly")
                : "Video";
            VisualStateManager.GoToState(this, contentVisualStateName, true);

            // Update lively options button visibility
            // TODO: Use XAML visual state to encode this logic
            LivelyOptionsButton.Visibility = ViewModel.PlayerVisibility == PlayerVisibilityState.Visible &&
                                             ViewModel.ViewMode != WindowViewMode.Compact &&
                                             ViewModel.AudioOnly
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void UpdatePreviewType()
        {
            if (ViewModel.PlayerVisibility == PlayerVisibilityState.Visible || ViewModel.ViewMode == WindowViewMode.Compact)
            {
                VisualStateManager.GoToState(this, "NoPreview", true);
            }
            else
            {
                VisualStateManager.GoToState(this, ViewModel.AudioOnly ? "AudioPreview" : "VideoPreview", true);
            }
        }

        private void UpdateMiniPlayerMargin()
        {
            if (ViewModel.PlayerVisibility == PlayerVisibilityState.Visible || ViewModel.ViewMode == WindowViewMode.Compact)
            {
                VisualStateManager.GoToState(this, "NoMargin", false);
            }
            else
            {
                switch (ViewModel.NavigationViewDisplayMode)
                {
                    case NavigationViewDisplayMode.Minimal when ViewModel.PlayerVisibility == PlayerVisibilityState.Hidden:
                        VisualStateManager.GoToState(this, "HiddenMinimalMargin", false);
                        break;
                    case NavigationViewDisplayMode.Minimal:
                        VisualStateManager.GoToState(this, "MinimalMargin", false);
                        break;
                    case NavigationViewDisplayMode.Compact when ViewModel.PlayerVisibility == PlayerVisibilityState.Hidden:
                    case NavigationViewDisplayMode.Expanded when ViewModel.PlayerVisibility == PlayerVisibilityState.Hidden:
                        VisualStateManager.GoToState(this, "HiddenNormalMargin", false);
                        break;
                    case NavigationViewDisplayMode.Compact:
                    case NavigationViewDisplayMode.Expanded:
                        VisualStateManager.GoToState(this, "NormalMargin", false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void PlayQueueFlyout_OnOpening(object sender, object e)
        {
            // Delay load PlaylistView until flyout is opening
            // Save loading time when launching from file
            FindName(nameof(PlaylistView));
        }

        private async void PlayQueueFlyout_OnOpened(object sender, object e)
        {
            if (PlaylistView == null) return;
            await PlaylistView.SmoothScrollActiveItemIntoViewAsync();
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            UpdateBackgroundAcrylicOpacity(ActualTheme);
        }

        private void UpdateBackgroundAcrylicOpacity(ElementTheme theme)
        {
            // Set in code due to XAML compiler not setting it in Release
            BackgroundAcrylicBrush.TintLuminosityOpacity = theme == ElementTheme.Light ? 0.5 : 0.4;
        }

        private void PlayQueueButton_OnDragEnter(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
            _delayFlyoutOpenTimer.Debounce(() => PlayQueueFlyout.ShowAt(PlayQueueButton), TimeSpan.FromMilliseconds(500));
        }

        private void PlayQueueButton_OnDragLeave(object sender, DragEventArgs e)
        {
            _delayFlyoutOpenTimer.Stop();
        }

        private void PlayerControlsBackground_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            PlayerControls.FocusFirstButton(FocusState.Pointer);
            e.Handled = true;
        }

        private async void VideoView_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // Reset focus after manipulation
            // Must be queued in Dispatcher or risk losing focus right after
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                () => PlayerControls.FocusFirstButton(FocusState.Programmatic));
        }

        private void ControlsVisibilityStates_OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == nameof(ControlsHidden))
            {
                // Handle Space key when the controls are not visible.
                // Also hide tooltip if there is any.
                HiddenButton.Focus(FocusState.Programmatic);
            }
        }

        private void VideoView_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.OnPlayerClick())
            {
                PlayerControls.FocusFirstButton();
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = Strings.Resources.Play;
        }
    }
}
