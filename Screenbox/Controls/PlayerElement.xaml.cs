#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using LibVLCSharp.Platforms.Windows;
using Screenbox.Core.ViewModels;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlayerElement : UserControl
    {
        public static readonly DependencyProperty ButtonMarginProperty = DependencyProperty.Register(
            nameof(ButtonMargin),
            typeof(Thickness),
            typeof(PlayerElement),
            new PropertyMetadata(default(Thickness)));

        public Thickness ButtonMargin
        {
            get => (Thickness)GetValue(ButtonMarginProperty);
            set => SetValue(ButtonMarginProperty, value);
        }

        public event RoutedEventHandler? Click;

        private GestureRecognizer _gestureRecognizer;

        internal PlayerElementViewModel ViewModel => (PlayerElementViewModel)DataContext;

        public PlayerElement()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerElementViewModel>();

            _gestureRecognizer = new GestureRecognizer
            {
                GestureSettings = GestureSettings.Hold | GestureSettings.HoldWithMouse |
                    GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateInertia,
            };
            _gestureRecognizer.ManipulationStarted += GestureRecognizer_OnManipulationStarted;
            _gestureRecognizer.ManipulationUpdated += GestureRecognizer_OnManipulationUpdated;
            _gestureRecognizer.ManipulationCompleted += GestureRecognizer_OnManipulationCompleted;
            _gestureRecognizer.Holding += GestureRecognizer_OnHolding;
        }

        private void VlcVideoView_OnInitialized(object sender, InitializedEventArgs e)
        {
            ViewModel.Initialize(e.SwapChainOptions);
        }

        private void VlcVideoView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.UpdatePlayerViewSize(e.NewSize);
        }

        private void VideoViewButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsEnabled) return;
            ViewModel.OnClick();
            Click?.Invoke(sender, e);
        }

        private void VideoViewButton_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!IsEnabled) return;
            ViewModel.OnClick();
            Click?.Invoke(sender, e);
        }

        private void VideoViewButton_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            _gestureRecognizer.CompleteGesture();
            VideoViewButton.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.IsHolding || !IsEnabled) return;

            _gestureRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(this));
        }

        private void VideoViewButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            _gestureRecognizer.ProcessDownEvent(e.GetCurrentPoint(this));
            VideoViewButton.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            _gestureRecognizer.ProcessUpEvent(e.GetCurrentPoint(this));
            VideoViewButton.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            var pointer = e.GetCurrentPoint(VideoViewButton);
            var properties = pointer.Properties;
            ViewModel.HandlePointerWheelInput(properties.MouseWheelDelta, properties.IsHorizontalMouseWheel);
            e.Handled = true;
        }

        private void GestureRecognizer_OnManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            ViewModel.OnManipulationStarted();
        }

        private void GestureRecognizer_OnManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            if (ViewModel.Is360Video && (args.CurrentContactCount == 2 || args.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse))
            {
                ViewModel.HandleSphericalPanGesture(args.Delta.Translation.X, args.Delta.Translation.Y);
            }
        }

        private void GestureRecognizer_OnManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            if (!ViewModel.IsPanning && args.ContactCount == 1)
            {
                ViewModel.HandleSwipeGesture(args.Cumulative.Translation.X, args.Cumulative.Translation.Y);
            }

            ViewModel.OnManipulationCompleted();
        }

        private void GestureRecognizer_OnHolding(GestureRecognizer sender, HoldingEventArgs args)
        {
            ViewModel.HandleHoldingGesture(args.HoldingState);
        }

        // private void PlayerElement_OnLoaded(object sender, RoutedEventArgs e)
        // {
        //     ViewModel.ClearViewRequested += ViewModelOnClearViewRequested;
        // }
        //
        // private void PlayerElement_OnUnloaded(object sender, RoutedEventArgs e)
        // {
        //     ViewModel.ClearViewRequested -= ViewModelOnClearViewRequested;
        // }
        //
        // private void ViewModelOnClearViewRequested(object sender, EventArgs e)
        // {
        //     VlcVideoView.Clear();
        // }
    }
}
