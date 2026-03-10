#nullable enable

using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Commands;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    internal HomePageViewModel ViewModel => (HomePageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    /// <summary>
    /// Wraps <see cref="CommonViewModel.OpenFilesCommand"/> with a
    /// <see cref="NotificationCommand"/> that sends a localized error notification on failure.
    /// </summary>
    public ICommand OpenFilesCommand { get; }

    public HomePage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<HomePageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();

        OpenFilesCommand = new NotificationCommand(
            Common.OpenFilesCommand,
            onFailure: e => Common.SendErrorMessage(Screenbox.Strings.Resources.FailedToOpenFilesNotificationTitle, e.Message));
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        VisualStateManager.GoToState(this, ViewModel.HasRecentMedia ? "RecentMedia" : "Welcome", false);
    }
}
