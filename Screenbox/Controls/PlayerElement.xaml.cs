#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using LibVLCSharp.Platforms.Windows;
using Screenbox.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
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

        internal PlayerInteractionViewModel InteractionViewModel { get; }

        private const VirtualKey PeriodKey = (VirtualKey)190;
        private const VirtualKey CommaKey = (VirtualKey)188;

        public PlayerElement()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayerElementViewModel>();
            InteractionViewModel = Ioc.Default.GetRequiredService<PlayerInteractionViewModel>();
        }

        private void VideoViewButton_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = Strings.Resources.Open;
        }

        private void VlcVideoView_OnInitialized(object sender, InitializedEventArgs e)
        {
            ViewModel.Initialize(e.SwapChainOptions);
        }

        private void VideoViewButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!IsEnabled) return;
            InteractionViewModel.OnClick();
            Click?.Invoke(sender, e);
            e.Handled = true;
        }
    }
}
