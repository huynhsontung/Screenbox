#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly INavigationService _navigationService;

        public NetworkPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationViewDisplayMode = navigationService.DisplayMode;
            _titleText = Strings.Resources.Network;
            Breadcrumbs = new ObservableCollection<string>();

            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;
        }

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }

        public ObservableCollection<string> Breadcrumbs { get; }

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
