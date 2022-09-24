#nullable enable

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
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal sealed partial class HomePageViewModel : ObservableRecipient,
        IRecipient<PlaylistActiveItemChangedMessage>
    {
        public ObservableCollection<MediaViewModel> Recent { get; }

        public bool HasRecentMedia => StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count > 0;

        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly MediaViewModelFactory _mediaFactory;

        public HomePageViewModel(INavigationService navigationService, MediaViewModelFactory mediaFactory)
        {
            _mediaFactory = mediaFactory;
            _navigationViewDisplayMode = navigationService.DisplayMode;
            Recent = new ObservableCollection<MediaViewModel>();

            navigationService.DisplayModeChanged += NavigationServiceOnDisplayModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        private void NavigationServiceOnDisplayModeChanged(object sender, NavigationServiceDisplayModeChangedEventArgs e)
        {
            NavigationViewDisplayMode = e.NewValue;
        }

        public async void Receive(PlaylistActiveItemChangedMessage message)
        {
            if (message.Value is not { Source: StorageFile }) return;
            await UpdateRecentMediaList().ConfigureAwait(false);
        }

        public static void AddToRecent(MediaViewModel media)
        {
            if (media.Source is not StorageFile file) return;
            string metadata = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            StorageApplicationPermissions.MostRecentlyUsedList.Add(file, metadata);
        }

        public async void OnLoaded()
        {
            await UpdateRecentMediaList().ConfigureAwait(false);
        }

        private async Task UpdateRecentMediaList()
        {
            IEnumerable<Task<StorageFile?>> tasks = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderByDescending(x => x.Metadata)
                .Select(x => ConvertMruTokenToStorageFile(x.Token));
            StorageFile[] files = (await Task.WhenAll(tasks)).OfType<StorageFile>().ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                StorageFile file = files[i];
                if (i >= Recent.Count)
                {
                    Recent.Add(_mediaFactory.GetSingleton(file));
                }
                else if (!file.IsEqual(Recent[i].Source as IStorageItem))
                {
                    // Find index of the VM of the same file
                    // There is no FindIndex method for ObservableCollection :(
                    int existingIndex = -1;
                    for (int j = i + 1; j < Recent.Count; j++)
                    {
                        if (file.IsEqual(Recent[j].Source as IStorageItem))
                        {
                            existingIndex = j;
                            break;
                        }
                    }

                    if (existingIndex == -1)
                    {
                        Recent.Insert(i, _mediaFactory.GetSingleton(file));
                    }
                    else
                    {
                        MediaViewModel toInsert = Recent[existingIndex];
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

            IEnumerable<Task> loadingTasks = Recent.Select(x => x.LoadDetailsAndThumbnailAsync());
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

        private static async Task<StorageFile?> ConvertMruTokenToStorageFile(string token)
        {
            try
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token,
                    AccessCacheOptions.UseReadOnlyCachedCopy | AccessCacheOptions.SuppressAccessTimeUpdate);
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }
    }
}
