#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using LibVLCSharp.Platforms.Windows;
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
        ViewModel.ProcessPointerWheelInput(properties.MouseWheelDelta, properties.IsHorizontalMouseWheel);
        e.Handled = true;
    }

    private void VideoViewButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (ViewModel.IsHolding || !IsEnabled) return;

        ViewModel.ProcessSlideGesture(e.Delta.Translation, e.Cumulative.Translation);
    }

    private void VideoViewButton_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        if (ViewModel.IsHolding || !IsEnabled) return;

        ViewModel.ProcessSwipeGesture(e.Cumulative.Translation);
        ViewModel.OnManipulationCompleted();
    }

    private void GestureRecognizer_OnHolding(GestureRecognizer sender, HoldingEventArgs args)
    {
        ViewModel.ProcessHoldingGesture(args.HoldingState);
    }
}
