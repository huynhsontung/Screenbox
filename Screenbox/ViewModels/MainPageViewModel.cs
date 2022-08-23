using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PlayerVisibilityChangedMessage>
    {
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public MainPageViewModel()
        {
            IsActive = true;
        }

        public void Receive(PlayerVisibilityChangedMessage message)
        {
            PlayerVisible = message.Value;
        }

        partial void OnNavigationViewDisplayModeChanged(NavigationViewDisplayMode value)
        {
            Messenger.Send(new NavigationViewDisplayModeChangedMessage(value));
        }
    }
}
