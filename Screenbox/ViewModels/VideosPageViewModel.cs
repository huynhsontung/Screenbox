#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class VideosPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private string _urlText;
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public ObservableCollection<string> Breadcrumbs { get; }

        private readonly IFilesService _filesService;

        public VideosPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _urlText = string.Empty;
            _titleText = Strings.Resources.Videos;
            Breadcrumbs = new ObservableCollection<string>();

            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        public void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UrlText) && Uri.TryCreate(UrlText, UriKind.Absolute, out Uri uri))
            {
                Messenger.Send(new PlayMediaMessage(uri));
            }
        }

        public async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            StorageFile? pickedFile = await _filesService.PickFileAsync();

            if (pickedFile != null)
                Messenger.Send(new PlayMediaMessage(pickedFile));
        }

        public void OnFolderViewFrameNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            UpdateBreadcrumbs(crumbs);
        }

        public void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? Strings.Resources.Videos;
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder.DisplayName);
            }
        }
    }
}
