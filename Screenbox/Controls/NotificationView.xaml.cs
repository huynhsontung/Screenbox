using System.Numerics;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class NotificationView : UserControl
    {
        private NotificationViewModel ViewModel => (NotificationViewModel)DataContext;

        public NotificationView()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<NotificationViewModel>();
        }

        private InfoBarSeverity ConvertInfoBarSeverity(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return InfoBarSeverity.Error;
                case NotificationLevel.Warning:
                    return InfoBarSeverity.Warning;
                case NotificationLevel.Success:
                    return InfoBarSeverity.Success;
                default:
                    return InfoBarSeverity.Informational;
            }
        }
    }
}
