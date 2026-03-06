#nullable enable

using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        internal HomePageViewModel ViewModel => (HomePageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        /// <summary>
        /// Code-behind command for opening media files.
        /// Catches errors and sends a localized error notification via the common view model.
        /// </summary>
        public IAsyncRelayCommand OpenFilesCommand { get; }

        public HomePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<HomePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();

            OpenFilesCommand = new AsyncRelayCommand(OpenFilesExecuteAsync);
        }

        private async System.Threading.Tasks.Task OpenFilesExecuteAsync()
        {
            try
            {
                await Common.OpenFilesAsync();
            }
            catch (Exception e)
            {
                Common.SendErrorMessage(Screenbox.Strings.Resources.FailedToOpenFilesNotificationTitle, e.Message);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            VisualStateManager.GoToState(this, ViewModel.HasRecentMedia ? "RecentMedia" : "Welcome", false);
        }
    }
}
