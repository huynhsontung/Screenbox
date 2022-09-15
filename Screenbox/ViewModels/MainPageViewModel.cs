using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PlayerVisibilityChangedMessage>
    {
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly INavigationService _navigationService;

        public MainPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            IsActive = true;
        }

        public void Receive(PlayerVisibilityChangedMessage message)
        {
            PlayerVisible = message.Value;
        }

        partial void OnNavigationViewDisplayModeChanged(NavigationViewDisplayMode value)
        {
            _navigationService.DisplayMode = value;
        }
    }
}
