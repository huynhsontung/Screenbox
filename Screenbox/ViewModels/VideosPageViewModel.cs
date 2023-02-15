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
using Screenbox.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Screenbox.ViewModels
{
    internal sealed partial class VideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private bool _isFileOnly;

        public ObservableCollection<StorageFolder> Breadcrumbs { get; }

        private readonly INavigationService _navigationService;
        private StorageLibrary? _library;

        public VideosPageViewModel(INavigationService navigationService, ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _titleText = Strings.Resources.Videos;
            Breadcrumbs = new ObservableCollection<StorageFolder>();
            _isFileOnly = !settingsService.ShowVideoFolders;
        }

        public void OnNavigatedTo()
        {
            if (IsFileOnly)
            {
                _navigationService.NavigateChild(typeof(VideosPageViewModel), typeof(AllVideosPageViewModel));
            }
            else
            {
                _navigationService.NavigateChild(typeof(VideosPageViewModel), typeof(FolderViewPageViewModel),
                    new[] { KnownFolders.VideosLibrary });
            }
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
            if (_library == null)
            {
                _library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
                if (!IsFileOnly)
                {
                    _library.DefinitionChanged += LibraryOnDefinitionChanged;
                }
            }

            await _library.RequestAddFolderAsync();
        }

        private void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            Messenger.Send(new RefreshFolderMessage());
        }
    }
}
