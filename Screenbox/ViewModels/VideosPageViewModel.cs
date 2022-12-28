#nullable enable

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

namespace Screenbox.ViewModels
{
    internal sealed partial class VideosPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public ObservableCollection<StorageFolder> Breadcrumbs { get; }

        private readonly INavigationService _navigationService;

        public VideosPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _titleText = Strings.Resources.Videos;
            Breadcrumbs = new ObservableCollection<StorageFolder>();

            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
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
    }
}
