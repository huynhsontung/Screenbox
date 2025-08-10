#nullable enable

using System;
using System.Threading.Tasks;
using Screenbox.Core.Helpers;
using Screenbox.Helpers;
using Screenbox.Strings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls
{
    public sealed partial class OpenUrlDialog : ContentDialog
    {
        public OpenUrlDialog()
        {
            this.DefaultStyleKey = typeof(ContentDialog);
            this.InitializeComponent();
            FlowDirection = GlobalizationHelper.GetFlowDirection();
            RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        }

        public static async Task<Uri?> GetUrlAsync()
        {
            OpenUrlDialog dialog = new();
            ContentDialogResult result = await dialog.ShowAsync();
            string url = dialog.UrlBox.Text;
            
            if (result != ContentDialogResult.Primary || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return null;

            // The validation is now handled inline during text input
            // If we reach here, the URL should be valid
            return uri;
        }

        private bool CanOpen(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return false;

            // Check if URL is supported
            if (!UrlHelpers.IsSupportedMediaUrl(uri))
                return false;

            return true;
        }

        private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string url = textBox.Text;
                
                // Clear error if text is empty
                if (string.IsNullOrWhiteSpace(url))
                {
                    ErrorMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    return;
                }

                // Validate URL format first
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    ErrorMessage.Text = "Please enter a valid URL.";
                    ErrorMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    return;
                }

                // Validate if URL is supported
                if (!UrlHelpers.IsSupportedMediaUrl(uri))
                {
                    ErrorMessage.Text = UrlHelpers.GetUnsupportedUrlMessage(uri);
                    ErrorMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    return;
                }

                // URL is valid and supported
                ErrorMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
