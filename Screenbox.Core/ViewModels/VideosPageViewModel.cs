#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using IResourceService = Screenbox.Core.Services.IResourceService;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class VideosPageViewModel : ObservableRecipient
    {
        public ObservableCollection<StorageFolder> Breadcrumbs { get; }

        public object NavigationParameter { get; }

        [ObservableProperty] private bool _hasVideos;

        private bool HasLibrary => _libraryService.VideosLibrary != null;

        private readonly ILibraryService _libraryService;
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;

        public VideosPageViewModel(ILibraryService libraryService, IResourceService resourceService)
        {
            _libraryService = libraryService;
            _resourceService = resourceService;
            _libraryService.VideosLibraryContentChanged += OnVideosLibraryContentChanged;
            _hasVideos = true;
            Breadcrumbs = new ObservableCollection<StorageFolder> { KnownFolders.VideosLibrary };
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            NavigationParameter = new NavigationMetadata(typeof(VideosPageViewModel), KnownFolders.VideosLibrary);
        }

        public void UpdateVideos()
        {
            HasVideos = _libraryService.GetVideosFetchResult().Count > 0;
            AddFolderCommand.NotifyCanExecuteChanged();
        }

        public void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            UpdateBreadcrumbs(crumbs);
        }

        private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null)
            {
                Breadcrumbs.Add(KnownFolders.VideosLibrary);
            }
            else
            {
                foreach (StorageFolder storageFolder in crumbs)
                {
                    Breadcrumbs.Add(storageFolder);
                }
            }
        }

        private void OnVideosLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(UpdateVideos);
        }

        [RelayCommand(CanExecute = nameof(HasLibrary))]
        private async Task AddFolder()
        {
            try
            {
                await _libraryService.VideosLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    _resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
            }
        }
    }
}
