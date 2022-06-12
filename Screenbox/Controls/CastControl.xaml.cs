using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class CastControl : UserControl
    {
        internal CastControlViewModel ViewModel => (CastControlViewModel)DataContext;

        private CastControl()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<CastControlViewModel>();
        }

        public static Flyout GetFlyout()
        {
            Flyout flyout = new();
            CastControl control = new();
            flyout.Content = control;
            flyout.Opened += (_, _) => control.ViewModel.StartDiscovering();
            flyout.Closed += (_, _) => control.ViewModel.StopDiscovering();
            return flyout;
        }
    }
}
