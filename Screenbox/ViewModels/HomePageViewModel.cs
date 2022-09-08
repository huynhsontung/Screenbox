using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;

namespace Screenbox.ViewModels
{
    internal partial class HomePageViewModel : ObservableRecipient,
        IRecipient<NavigationViewDisplayModeChangedMessage>,
        IRecipient<PlaylistActiveItemChangedMessage>
    {
        public ObservableCollection<StorageItemViewModel> Recent { get; }

        public bool HasRecentMedia => StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count > 0;

        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        public HomePageViewModel()
        {
            _navigationViewDisplayMode = Messenger.Send(new NavigationViewDisplayModeRequestMessage());
            Recent = new ObservableCollection<StorageItemViewModel>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(NavigationViewDisplayModeChangedMessage message)
        {
            NavigationViewDisplayMode = message.Value;
        }

        public async void Receive(PlaylistActiveItemChangedMessage message)
        {
            if (message.Value is not { Source: StorageFile file }) return;
            string metadata = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            StorageApplicationPermissions.MostRecentlyUsedList.Add(file, metadata);
            await UpdateRecentMediaList().ConfigureAwait(false);
        }

        public async void OnLoaded()
        {
            await UpdateRecentMediaList().ConfigureAwait(false);
        }

        private async Task UpdateRecentMediaList()
        {
            IEnumerable<Task<StorageFile>> tasks = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderByDescending(x => x.Metadata)
                .Select(x => ConvertMruTokenToStorageFile(x.Token));
            StorageFile[] files = await Task.WhenAll(tasks);
            for (int i = 0; i < files.Length; i++)
            {
                StorageFile file = files[i];
                if (i >= Recent.Count)
                {
                    Recent.Add(new StorageItemViewModel(file));
                }
                else if (!file.IsEqual(Recent[i].StorageItem))
                {
                    // Find index of the VM of the same file
                    // There is no FindIndex method for ObservableCollection :(
                    int existingIndex = -1;
                    for (int j = i + 1; j < Recent.Count; j++)
                    {
                        if (file.IsEqual(Recent[j].StorageItem))
                        {
                            existingIndex = j;
                            break;
                        }
                    }

                    if (existingIndex == -1)
                    {
                        Recent.Insert(i, new StorageItemViewModel(file));
                    }
                    else
                    {
                        StorageItemViewModel toInsert = Recent[existingIndex];
                        Recent.RemoveAt(existingIndex);
                        Recent.Insert(i, toInsert);
                    }
                }
            }

            // Remove stale items
            while (Recent.Count > files.Length)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }

            IEnumerable<Task> loadingTasks = Recent.Select(x => x.Media!.LoadDetailsAndThumbnailAsync());
            await Task.WhenAll(loadingTasks).ConfigureAwait(false);
        }

        [RelayCommand]
        private void Play(StorageItemViewModel item)
        {
            if (item.Media != null)
            {
                Messenger.Send(new PlayMediaMessage(item.Media));
            }
        }

        private static Task<StorageFile> ConvertMruTokenToStorageFile(string token)
        {
            return StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token).AsTask();
        }
    }
}
