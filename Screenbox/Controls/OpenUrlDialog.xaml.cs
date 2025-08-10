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

            // Validate if the URL is supported for media playback
            if (!UrlHelpers.IsSupportedMediaUrl(uri))
            {
                // Show error message for unsupported URLs
                string errorMessage = UrlHelpers.GetUnsupportedUrlMessage(uri);
                ContentDialog errorDialog = new()
                {
                    Title = Resources.UnsupportedUrlErrorTitle,
                    Content = errorMessage,
                    CloseButtonText = Resources.Close,
                    DefaultButton = ContentDialogButton.Close,
                    FlowDirection = GlobalizationHelper.GetFlowDirection(),
                    RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme
                };
                
                await errorDialog.ShowAsync();
                return null;
            }

            return uri;
        }

        private bool CanOpen(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri _);
        }
    }
}
