using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Controls;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class MainPageViewModel : ObservableRecipient,
        IRecipient<PlayerVisibilityChangedMessage>,
        IRecipient<NavigationViewDisplayModeRequestMessage>
    {
        [ObservableProperty] private bool _playerVisible;
        [ObservableProperty] private bool _shouldUseMargin;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private NavigationViewDisplayMode _navigationViewDisplayMode;

        public MainPageViewModel()
        {
            IsActive = true;
        }

        public void Receive(PlayerVisibilityChangedMessage message)
        {
            PlayerVisible = message.Value == PlayerVisibilityStates.Visible;
            ShouldUseMargin = message.Value != PlayerVisibilityStates.Hidden;
        }

        public void Receive(NavigationViewDisplayModeRequestMessage message)
        {
            message.Reply(NavigationViewDisplayMode);
        }
    }
}
