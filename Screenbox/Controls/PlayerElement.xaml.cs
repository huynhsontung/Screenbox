#nullable enable

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PlayerElement : UserControl
    {
        public event RoutedEventHandler? Click;

        internal PlayerElementViewModel ViewModel => (PlayerElementViewModel)DataContext;

        internal PlayerInteractionViewModel InteractionViewModel { get; }

        private const VirtualKey PeriodKey = (VirtualKey)190;
        private const VirtualKey CommaKey = (VirtualKey)188;
        private const VirtualKey PlusKey = (VirtualKey)0xBB;
        private const VirtualKey MinusKey = (VirtualKey)0xBD;
        private const VirtualKey AddKey = (VirtualKey)0x6B;
        private const VirtualKey SubtractKey = (VirtualKey)0x6D;

        public PlayerElement()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<PlayerElementViewModel>();
            InteractionViewModel = App.Services.GetRequiredService<PlayerInteractionViewModel>();
            VideoViewButton.Click += (sender, args) => Click?.Invoke(sender, args);
            VideoViewButton.Drop += (_, _) => VideoViewButton.Focus(FocusState.Programmatic);
        }
    }
}
