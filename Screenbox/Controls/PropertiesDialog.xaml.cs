#nullable enable

using Screenbox.Core.Common;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls
{
    public sealed partial class PropertiesDialog : ContentDialog, IPropertiesDialog
    {
        public static readonly DependencyProperty MediaProperty = DependencyProperty.Register(
            nameof(Media),
            typeof(MediaViewModel),
            typeof(PropertiesDialog),
            new PropertyMetadata(null));

        public MediaViewModel? Media
        {
            get => (MediaViewModel?)GetValue(MediaProperty);
            set => SetValue(MediaProperty, value);
        }

        public PropertiesDialog()
        {
            this.InitializeComponent();
            FlowDirection = GlobalizationHelper.GetFlowDirection();
            RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        }
    }
}
