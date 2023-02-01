﻿using Windows.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;
using Screenbox.Controls;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class CommonViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityStates>>
    {
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
        [ObservableProperty] private Thickness _scrollBarMargin;
        [ObservableProperty] private Thickness _footerBottomPaddingMargin;
        [ObservableProperty] private double _footerBottomPaddingHeight;

        private readonly INavigationService _navigationService;

        public CommonViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityStates> message)
        {
            ScrollBarMargin = message.NewValue == PlayerVisibilityStates.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageScrollBarMargin"];

            FooterBottomPaddingMargin = message.NewValue == PlayerVisibilityStates.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageBottomMargin"];

            FooterBottomPaddingHeight = message.NewValue == PlayerVisibilityStates.Hidden
                ? 0
                : (double)Application.Current.Resources["ContentPageBottomPaddingHeight"];
        }

        [RelayCommand]
        private void OpenAlbum(AlbumViewModel? album)
        {
            if (album == null) return;
            _navigationService.Navigate(typeof(AlbumDetailsPageViewModel), album);
        }
    }
}
