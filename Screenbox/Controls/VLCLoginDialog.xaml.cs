#nullable enable

using Screenbox.Core;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls
{
    public sealed partial class VLCLoginDialog : ContentDialog, IVlcLoginDialog
    {
        public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
            nameof(Username),
            typeof(string),
            typeof(VLCLoginDialog),
            new PropertyMetadata(null));

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(VLCLoginDialog),
            new PropertyMetadata(null));

        public string? Text { get; set; }

        public string? Username
        {
            get => (string)GetValue(UsernameProperty);
            set => SetValue(UsernameProperty, value);
        }

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool StoreCredential { get; set; }

        public bool AskStoreCredential { get; set; }

        public VLCLoginDialog()
        {
            this.InitializeComponent();
            FlowDirection = GlobalizationHelper.GetFlowDirection();
            RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        }
    }
}
