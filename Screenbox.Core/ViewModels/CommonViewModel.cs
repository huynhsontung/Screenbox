#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class CommonViewModel : ObservableRecipient,
        IRecipient<SettingsChangedMessage>,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
    {
        public Dictionary<Type, string> NavigationStates { get; }

        public bool IsAdvancedModeEnabled => _settingsService.AdvancedMode;

        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
        [ObservableProperty] private Thickness _scrollBarMargin;
        [ObservableProperty] private Thickness _footerBottomPaddingMargin;
        [ObservableProperty] private double _footerBottomPaddingHeight;

        private readonly INavigationService _navigationService;
        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private readonly ISettingsService _settingsService;
        private readonly Dictionary<string, object> _pageStates;

        public CommonViewModel(INavigationService navigationService,
            IFilesService filesService,
            IResourceService resourceService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _filesService = filesService;
            _resourceService = resourceService;
            _settingsService = settingsService;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            NavigationStates = new Dictionary<Type, string>();
            _pageStates = new Dictionary<string, object>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName == nameof(SettingsPageViewModel.Theme) &&
                Window.Current.Content is Frame rootFrame)
            {
                rootFrame.RequestedTheme = _settingsService.Theme.ToElementTheme();
            }
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            this.NavigationViewDisplayMode = message.NewValue;
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            ScrollBarMargin = message.NewValue == PlayerVisibilityState.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageScrollBarMargin"];

            FooterBottomPaddingMargin = message.NewValue == PlayerVisibilityState.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageBottomMargin"];

            FooterBottomPaddingHeight = message.NewValue == PlayerVisibilityState.Hidden
                ? 0
                : (double)Application.Current.Resources["ContentPageBottomPaddingHeight"];
        }

        public void SavePageState(object state, string pageTypeName, int backStackDepth)
        {
            _pageStates[pageTypeName + backStackDepth] = state;
        }

        public bool TryGetPageState(string pageTypeName, int backStackDepth, out object state)
        {
            return _pageStates.TryGetValue(pageTypeName + backStackDepth, out state);
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }

        [RelayCommand]
        private void OpenAlbum(AlbumViewModel? album)
        {
            if (album == null) return;
            _navigationService.Navigate(typeof(AlbumDetailsPageViewModel),
                new NavigationMetadata(typeof(MusicPageViewModel), album));
        }

        [RelayCommand]
        private void OpenArtist(ArtistViewModel? artist)
        {
            if (artist == null) return;
            _navigationService.Navigate(typeof(ArtistDetailsPageViewModel),
                new NavigationMetadata(typeof(MusicPageViewModel), artist));
        }

        [RelayCommand]
        private async Task OpenFilesAsync()
        {
            try
            {
                IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync();
                if (files == null || files.Count == 0) return;
                Messenger.Send(new PlayMediaMessage(files));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToOpenFilesNotificationTitle), e.Message));
            }
        }
    }
}
