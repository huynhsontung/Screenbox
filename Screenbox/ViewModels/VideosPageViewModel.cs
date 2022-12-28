#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Screenbox.ViewModels
{
    internal sealed partial class VideosPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
        [ObservableProperty] private bool _isFileOnly;

        public ObservableCollection<StorageFolder> Breadcrumbs { get; }

        private readonly INavigationService _navigationService;
        private readonly IFilesService _filesService;
        private readonly ISettingsService _settingsService;

        public VideosPageViewModel(INavigationService navigationService,
            IFilesService filesService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _filesService = filesService;
            _settingsService = settingsService;
            _titleText = Strings.Resources.Videos;
            Breadcrumbs = new ObservableCollection<StorageFolder>();

            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            _isFileOnly = !settingsService.ShowVideoFolders;

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        public void OnNavigatedTo()
        {
            _navigationService.NavigateChild(typeof(VideosPageViewModel), typeof(FolderViewPageViewModel),
                IsFileOnly ? _filesService.GetVideosFromLibrary() : new[] { KnownFolders.VideosLibrary });
        }

        public void OnFolderViewFrameNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            UpdateBreadcrumbs(crumbs);
        }

        public void OnBreadcrumbBarItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(args.Index + 1).ToArray();
            _navigationService.NavigateChild(typeof(VideosPageViewModel), typeof(FolderViewPageViewModel), crumbs);
        }

        private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? Strings.Resources.Videos;
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder);
            }
        }

        [RelayCommand]
        private async Task AddFolder()
        {
            StorageLibrary? library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            if (library == null) return;
            await library.RequestAddFolderAsync();
        }
    }
}
