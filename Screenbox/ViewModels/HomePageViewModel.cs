#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;

namespace Screenbox.ViewModels
{
    internal sealed partial class HomePageViewModel : ObservableRecipient,
        IRecipient<PlaylistActiveItemChangedMessage>,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        public ObservableCollection<MediaViewModelWithMruToken> Recent { get; }

        public bool HasRecentMedia => StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count > 0;

        [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;

        private readonly MediaViewModelFactory _mediaFactory;

        public HomePageViewModel(MediaViewModelFactory mediaFactory)
        {
            _mediaFactory = mediaFactory;
            _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            Recent = new ObservableCollection<MediaViewModelWithMruToken>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
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
            Tuple<string, Task<StorageFile?>>[] tuples = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderByDescending(x => x.Metadata)
                .Select(x => new Tuple<string, Task<StorageFile?>>(x.Token, ConvertMruTokenToStorageFile(x.Token)))
                .ToArray();
            for (int i = 0; i < tuples.Length; i++)
            {
                StorageFile? file = await tuples[i].Item2;
                string token = tuples[i].Item1;
                if (file == null)
                {
                    StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
                    continue;
                }

                if (i >= Recent.Count)
                {
                    Recent.Add(new MediaViewModelWithMruToken(token, _mediaFactory.GetSingleton(file)));
                }
                else if (!file.IsEqual(Recent[i].Media.Source as IStorageItem))
                {
                    // Find index of the VM of the same file
                    // There is no FindIndex method for ObservableCollection :(
                    int existingIndex = -1;
                    for (int j = i + 1; j < Recent.Count; j++)
                    {
                        if (file.IsEqual(Recent[j].Media.Source as IStorageItem))
                        {
                            existingIndex = j;
                            break;
                        }
                    }

                    if (existingIndex == -1)
                    {
                        Recent.Insert(i, new MediaViewModelWithMruToken(token, _mediaFactory.GetSingleton(file)));
                    }
                    else
                    {
                        MediaViewModelWithMruToken toInsert = Recent[existingIndex];
                        Recent.RemoveAt(existingIndex);
                        Recent.Insert(i, toInsert);
                    }
                }
            }

            // Remove stale items
            while (Recent.Count > tuples.Length)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }

            IEnumerable<Task> loadingTasks = Recent.Select(x => x.Media.LoadDetailsAndThumbnailAsync());
            await Task.WhenAll(loadingTasks).ConfigureAwait(false);
        }

        [RelayCommand]
        private void Play(MediaViewModelWithMruToken media)
        {
            Messenger.Send(new PlayMediaMessage(media.Media));
        }

        [RelayCommand]
        private void PlayNext(MediaViewModelWithMruToken media)
        {
            Messenger.SendPlayNext(media.Media);
        }

        [RelayCommand]
        private void Remove(MediaViewModelWithMruToken media)
        {
            Recent.Remove(media);
            StorageApplicationPermissions.MostRecentlyUsedList.Remove(media.Token);
        }

        [RelayCommand]
        private async Task ShowPropertiesAsync(MediaViewModelWithMruToken media)
        {
            ContentDialog propertiesDialog = PropertiesView.GetDialog(media.Media);
            await propertiesDialog.ShowAsync();
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