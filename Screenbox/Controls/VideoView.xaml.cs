#nullable enable

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class VideoView : UserControl
    {
        public event RoutedEventHandler? Click;

        internal VideoViewViewModel ViewModel => (VideoViewViewModel)DataContext;

        private const VirtualKey PeriodKey = (VirtualKey)190;
        private const VirtualKey CommaKey = (VirtualKey)188;

        public VideoView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VideoViewViewModel>();
            VideoViewButton.Click += (sender, args) => Click?.Invoke(sender, args);
            VideoViewButton.Drop += (_, _) => VideoViewButton.Focus(FocusState.Programmatic);
        }
    }
}
