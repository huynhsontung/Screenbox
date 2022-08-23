using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal partial class HomePageViewModel : ObservableRecipient,
        IRecipient<NavigationViewDisplayModeChangedMessage>
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public HomePageViewModel()
        {
            _navigationViewDisplayMode = Messenger.Send(new NavigationViewDisplayModeRequestMessage());

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(NavigationViewDisplayModeChangedMessage message)
        {
            NavigationViewDisplayMode = message.Value;
        }
    }
}
