#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Screenbox.ViewModels
{
    internal partial class VideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _urlText;

        [ObservableProperty] private string _titleText;

        public ObservableCollection<string> Breadcrumbs { get; }

        private readonly IFilesService _filesService;

        public VideosPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _urlText = string.Empty;
            _titleText = Strings.Resources.Videos;
            Breadcrumbs = new ObservableCollection<string>();
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

        public async Task<IReadOnlyList<StorageFolder>?> GetInitialBreadcrumbsAsync()
        {
            StorageLibrary? library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            if (library == null || library.Folders.Count <= 0) return null;
            if (library.Folders.Count == 1)
            {
                return new[] { library.Folders[0] };
            }

            return null;
        }
    }
}
