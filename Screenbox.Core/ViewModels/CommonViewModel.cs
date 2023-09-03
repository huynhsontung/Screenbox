#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Common;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class CommonViewModel : ObservableRecipient,
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
        private readonly Func<IPropertiesDialog> _propertiesDialogFactory;
        private readonly Dictionary<string, double> _scrollingStates;

        public CommonViewModel(INavigationService navigationService,
            IFilesService filesService,
            IResourceService resourceService,
            ISettingsService settingsService,
            Func<IPropertiesDialog> propertiesDialogFactory)
        {
            _navigationService = navigationService;
            _filesService = filesService;
            _resourceService = resourceService;
            _settingsService = settingsService;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            _propertiesDialogFactory = propertiesDialogFactory;
            NavigationStates = new Dictionary<Type, string>();
            _scrollingStates = new Dictionary<string, double>();

            // Activate the view model's messenger
            IsActive = true;
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

        public void SaveScrollingState(ListViewBase element, Page page)
        {
            if (element.FindDescendant<ScrollViewer>() is { } scrollViewer)
                _scrollingStates[page.GetType().Name + page.Frame.BackStackDepth] = scrollViewer.VerticalOffset;
        }

        public bool TryRestoreScrollingStateOnce(ListViewBase element, Page page)
        {
            string key = page.GetType().Name + page.Frame.BackStackDepth;
            if (_scrollingStates.TryGetValue(key, out double verticalOffset))
            {
                _scrollingStates.Remove(key);
                return element.FindDescendant<ScrollViewer>()?.ChangeView(null, verticalOffset, null, true) ?? false;
            }

            return false;
        }

        private bool HasMedia(MediaViewModel? media) => media != null;

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

        [RelayCommand(CanExecute = nameof(HasMedia))]
        private async Task ShowPropertiesAsync(MediaViewModel? media)
        {
            if (media == null) return;
            IPropertiesDialog dialog = _propertiesDialogFactory();
            dialog.Media = media;
            await dialog.ShowAsync();
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
