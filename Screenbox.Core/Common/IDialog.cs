using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.Common
{
    public interface IDialog
    {
        object Title { get; set; }
        ContentDialogButton DefaultButton { get; set; }
        IAsyncOperation<ContentDialogResult> ShowAsync();
    }
}
