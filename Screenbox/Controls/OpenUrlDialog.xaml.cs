#nullable enable

using System;
using System.Threading.Tasks;
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
        }

        public static async Task<Uri?> GetUrlAsync()
        {
            OpenUrlDialog dialog = new();
            ContentDialogResult result = await dialog.ShowAsync();
            return result != ContentDialogResult.Primary ? null : new Uri(dialog.UrlBox.Text);
        }

        private bool CanOpen(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }
    }
}
