#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace Screenbox.ViewModels
{
    internal sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;

        public NetworkPageViewModel()
        {
            _titleText = Strings.Resources.Network;
            Breadcrumbs = new ObservableCollection<string>();
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
