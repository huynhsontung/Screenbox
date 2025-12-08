#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using LibVLCSharp.Platforms.Windows;
using Screenbox.Core.ViewModels;
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

        public PlayerElement()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerElementViewModel>();
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

        private void VideoViewButton_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ViewModel.ProcessTranslationManipulation(e.Delta.Translation.X,
                e.Delta.Translation.Y,
                e.Cumulative.Translation.X,
                e.Cumulative.Translation.Y);
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
