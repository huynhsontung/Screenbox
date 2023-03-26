#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _titleText;

        private readonly IResourceService _resourceService;

        public NetworkPageViewModel(IResourceService resourceService)
        {
            _resourceService = resourceService;
            _titleText = resourceService.GetString(ResourceName.Network);
            Breadcrumbs = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Breadcrumbs { get; }

        public void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? _resourceService.GetString(ResourceName.Network);
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder.DisplayName);
            }
        }
    }
}
