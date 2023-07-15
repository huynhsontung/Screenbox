#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;

        public NetworkPageViewModel()
        {
            _titleText = string.Empty;
            Breadcrumbs = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Breadcrumbs { get; }

        public void OnNavigatedTo(object? parameter)
        {
            switch (parameter)
            {
                case NavigationMetadata { Parameter: IReadOnlyList<StorageFolder> crumbs }:
                    UpdateBreadcrumbs(crumbs);
                    break;
                case IReadOnlyList<StorageFolder> crumbs:
                    UpdateBreadcrumbs(crumbs);
                    break;
            }
        }

        private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? string.Empty;
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder.DisplayName);
            }
        }
    }
}
