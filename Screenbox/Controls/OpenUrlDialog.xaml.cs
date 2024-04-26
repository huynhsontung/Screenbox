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
            string url = dialog.UrlBox.Text;
            return result == ContentDialogResult.Primary && Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                ? uri
                : null;
        }

        private bool CanOpen(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri _);
        }
    }
}