using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

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

    private readonly GestureRecognizer _gestureRecognizer;
    private bool _shouldSuppressNextClick;
    private uint? _trackedPointerId;

    public event RoutedEventHandler? Click;

    internal PlayerElementViewModel ViewModel => (PlayerElementViewModel)DataContext;

    public PlayerElement()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlayerElementViewModel>();

        _gestureRecognizer = new GestureRecognizer
        {
            GestureSettings = GestureSettings.Hold | GestureSettings.HoldWithMouse,
        };

        _gestureRecognizer.Holding += GestureRecognizer_OnHolding;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_gestureRecognizer is not null)
        {
            _gestureRecognizer.CompleteGesture();
            _gestureRecognizer.GestureSettings = GestureSettings.None;
            _gestureRecognizer.Holding -= GestureRecognizer_OnHolding;
        }
    }

    private void VlcVideoView_OnInitialized(object sender, VideoViewInitializedEventArgs e)
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
        if (_shouldSuppressNextClick)
        {
            _shouldSuppressNextClick = false;
            return;
        }

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
        CompleteGestureRecognizerGesture();
        VideoViewButton.ReleasePointerCapture(e.Pointer);
        e.Handled = IsEnabled;
    }

    private void VideoViewButton_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!IsEnabled || ViewModel.IsHolding || _trackedPointerId != e.Pointer.PointerId || !_gestureRecognizer.IsActive)
            return;

        try
        {
            _gestureRecognizer.ProcessMoveEvents(e.GetIntermediatePoints(this));
        }
        catch (Exception ex) when (ex is ArgumentException or System.Runtime.InteropServices.COMException)
        {
            CompleteGestureRecognizerGesture();
        }
    }

    private void VideoViewButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (_gestureRecognizer.IsActive)
            CompleteGestureRecognizerGesture();

        try
        {
            _gestureRecognizer.ProcessDownEvent(e.GetCurrentPoint(this));
        }
        catch (Exception ex) when (ex is ArgumentException or System.Runtime.InteropServices.COMException)
        {
            CompleteGestureRecognizerGesture();
            return;
        }

        _trackedPointerId = e.Pointer.PointerId;
        VideoViewButton.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void VideoViewButton_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Skip if no down event was processed, and recover gracefully if the
        // recognizer cannot process the event (e.g. non-chronological timestamps).
        if (IsEnabled && _trackedPointerId == e.Pointer.PointerId && _gestureRecognizer.IsActive)
        {
            try
            {
                _gestureRecognizer.ProcessUpEvent(e.GetCurrentPoint(this));
            }
            catch (Exception ex) when (ex is ArgumentException or System.Runtime.InteropServices.COMException)
            {
                CompleteGestureRecognizerGesture();
            }
        }

        if (_trackedPointerId == e.Pointer.PointerId)
        {
            _trackedPointerId = null;
        }

        VideoViewButton.ReleasePointerCapture(e.Pointer);
        e.Handled = IsEnabled;
    }

    private void VideoViewButton_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!IsEnabled) return;

        var pointer = e.GetCurrentPoint(VideoViewButton);
        var properties = pointer.Properties;
        ViewModel.ProcessPointerWheelInput(properties.MouseWheelDelta, properties.IsHorizontalMouseWheel);
        ViewModel.OnManipulationCompleted();
        e.Handled = true;
    }

    private void VideoViewButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (!IsEnabled) return;

        ViewModel.ProcessSlideGesture(e.Delta.Translation, e.Cumulative.Translation);
    }

    private void VideoViewButton_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        if (!IsEnabled) return;

        ViewModel.ProcessSwipeGesture(e.Cumulative.Translation);
        ViewModel.OnManipulationCompleted();
    }

    private void GestureRecognizer_OnHolding(GestureRecognizer sender, HoldingEventArgs args)
    {
        if (args.PointerDeviceType is not (Windows.Devices.Input.PointerDeviceType.Touch or Windows.Devices.Input.PointerDeviceType.Pen)
            && args.HoldingState is HoldingState.Completed or HoldingState.Canceled)
            _shouldSuppressNextClick = true;
        ViewModel.ProcessHoldingGesture(args.HoldingState);
    }

    private void CompleteGestureRecognizerGesture()
    {
        _trackedPointerId = null;
        _gestureRecognizer.CompleteGesture();
    }
}
