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

        internal PlayerElementViewModel ViewModel => (PlayerElementViewModel)DataContext;

        private GestureRecognizer _gestureRecognizer;

        public PlayerElement()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerElementViewModel>();

            _gestureRecognizer = new GestureRecognizer
            {
                GestureSettings = GestureSettings.Hold | GestureSettings.HoldWithMouse |
                    GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY,
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

            _gestureRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(VideoViewButton));
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            _gestureRecognizer.ProcessDownEvent(e.GetCurrentPoint(VideoViewButton));
            VideoViewButton.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            _gestureRecognizer.ProcessUpEvent(e.GetCurrentPoint(VideoViewButton));
            VideoViewButton.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void VideoViewButton_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!IsEnabled) return;

            var pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            ViewModel.HandlePointerWheelInput(
                pointer.Properties.MouseWheelDelta,
                pointer.Properties.IsHorizontalMouseWheel);
            e.Handled = true;
        }

        private void GestureRecognizer_OnManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            ViewModel.ManipulationStarted();
        }

        private void GestureRecognizer_OnManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            ViewModel.HandleManipulationGesture(
                args.Delta.Translation.X,
                args.Delta.Translation.Y,
                args.Cumulative.Translation.X,
                args.Cumulative.Translation.Y);
        }

        private void GestureRecognizer_OnManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            ViewModel.ManipulationCompleted();
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
