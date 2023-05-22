using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class CastControl : UserControl
    {
        internal CastControlViewModel ViewModel => (CastControlViewModel)DataContext;

        private CastControl()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<CastControlViewModel>();
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
