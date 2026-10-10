using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.Services;
using Screenbox.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Dialogs;

public sealed partial class DeleteMediaDialog : ContentDialog
{
    internal ISettingsService SettingsService { get; }

    private string MediaName { get; }

    [DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    public DeleteMediaDialog(string mediaName)
    {
        this.DefaultStyleKey = typeof(ContentDialog);
        this.InitializeComponent();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        SettingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        MediaName = mediaName;
    }
}
