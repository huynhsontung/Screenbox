using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Contexts;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Dialogs;

public sealed partial class DeleteMediaDialog : ContentDialog
{
    internal GeneralSettingsContext GeneralSettings { get; }

    private string MediaName { get; }

    [DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    public DeleteMediaDialog(string mediaName)
    {
        this.DefaultStyleKey = typeof(ContentDialog);
        this.InitializeComponent();
        GeneralSettings = Ioc.Default.GetRequiredService<GeneralSettingsContext>();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        MediaName = mediaName;
    }
}
