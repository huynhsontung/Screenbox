using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class SettingsPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        [ObservableProperty] private int _playerAutoResize;
        [ObservableProperty] private bool _playerVolumeGesture;
        [ObservableProperty] private bool _playerSeekGesture;
        [ObservableProperty] private bool _playerTapGesture;

        private readonly ISettingsService _settingsService;

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            _playerAutoResize = (int)settingsService.PlayerAutoResize;
            _playerVolumeGesture = settingsService.PlayerVolumeGesture;
            _playerSeekGesture = settingsService.PlayerSeekGesture;

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            _settingsService.PlayerAutoResize = (PlayerAutoResizeOptions)value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerAutoResize)));
        }

        partial void OnPlayerVolumeGestureChanged(bool value)
        {
            _settingsService.PlayerVolumeGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerVolumeGesture)));
        }

        partial void OnPlayerSeekGestureChanged(bool value)
        {
            _settingsService.PlayerSeekGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerSeekGesture)));
        }

        partial void OnPlayerTapGestureChanged(bool value)
        {
            _settingsService.PlayerTapGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(PlayerTapGesture)));
        }
    }
}
