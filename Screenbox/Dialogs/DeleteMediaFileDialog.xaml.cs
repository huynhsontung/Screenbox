using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Dialogs;

// copied the other dialogs

public sealed partial class DeleteMediaFileDialog : ContentDialog
{
    private string MediaName { get; }

    public bool SkipWarning => SkipWarningCheckBox.IsChecked == true;

    public DeleteMediaFileDialog(string mediaName)
    {
        this.DefaultStyleKey = typeof(ContentDialog);
        this.InitializeComponent();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        MediaName = mediaName;
    }
}
