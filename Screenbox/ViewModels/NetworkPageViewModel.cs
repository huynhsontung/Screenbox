#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal sealed partial class NetworkPageViewModel : ObservableRecipient,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private string _titleText;
        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public NetworkPageViewModel()
        {
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            _titleText = Strings.Resources.Network;
            Breadcrumbs = new ObservableCollection<string>();

            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
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
