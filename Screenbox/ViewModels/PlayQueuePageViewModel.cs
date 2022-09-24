using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class PlayQueuePageViewModel : ObservableRecipient
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly INavigationService _navigationService;

        public PlayQueuePageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationViewDisplayMode = navigationService.DisplayMode;

            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }
    }
}
