using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class NotificationView : UserControl
    {
        private NotificationViewModel ViewModel => (NotificationViewModel)DataContext;

        public NotificationView()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<NotificationViewModel>();
        }
    }
}
