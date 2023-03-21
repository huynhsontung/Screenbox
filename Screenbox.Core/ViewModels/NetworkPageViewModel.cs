#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;

        public NetworkPageViewModel()
        {
            _titleText = ResourceHelper.GetString(ResourceHelper.Network);
            Breadcrumbs = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Breadcrumbs { get; }

        public void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? ResourceHelper.GetString(ResourceHelper.Network);
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder.DisplayName);
            }
        }
    }
}
