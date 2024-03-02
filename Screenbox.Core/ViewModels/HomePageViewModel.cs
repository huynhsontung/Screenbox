﻿#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class HomePageViewModel : ObservableRecipient,
        IRecipient<PlaylistCurrentItemChangedMessage>
    {
        public ObservableCollection<MediaViewModel> Recent { get; }

        public bool HasRecentMedia => StorageApplicationPermissions.MostRecentlyUsedList.Entries.Count > 0 && _settingsService.ShowRecent;

        private readonly MediaViewModelFactory _mediaFactory;
        private readonly IFilesService _filesService;
        private readonly ILibraryService _libraryService;
        private readonly ISettingsService _settingsService;
        private readonly CoreDispatcher _dispatcher;
        private readonly Dictionary<string, string> _pathToMruMappings;
        private bool _isLoaded; // Assume this class is a singleton

        public HomePageViewModel(MediaViewModelFactory mediaFactory,
            IFilesService filesService,
            ISettingsService settingsService,
            ILibraryService libraryService)
        {
            _mediaFactory = mediaFactory;
            _filesService = filesService;
            _settingsService = settingsService;
            _libraryService = libraryService;
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            _pathToMruMappings = new Dictionary<string, string>();
            Recent = new ObservableCollection<MediaViewModel>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public async void Receive(PlaylistCurrentItemChangedMessage message)
        {
            if (_settingsService.ShowRecent)
            {
                await UpdateRecentMediaListAsync(false).ConfigureAwait(false);
            }
        }

        public async void OnLoaded()
        {
            // Only run once. Assume this class is a singleton.
            if (_isLoaded) return;
            _isLoaded = true;
            await UpdateContentAsync();
        }

        public async Task OnDrop(DragEventArgs e)
        {
            try
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        Messenger.Send(new PlayFilesWithNeighborsMessage(items, null));
                        return;
                    }
                }

                if (e.DataView.Contains(StandardDataFormats.WebLink))
                {
                    Uri? uri = await e.DataView.GetWebLinkAsync();
                    if (uri.IsFile)
                    {
                        Messenger.Send(new PlayMediaMessage(uri));
                    }
                }
            }
            catch (Exception exception)
            {
                Messenger.Send(new MediaLoadFailedNotificationMessage(exception.Message, string.Empty));
            }
        }

        [RelayCommand]
        private void OpenUrl(Uri? url)
        {
            if (url == null) return;
            Messenger.Send(new PlayMediaMessage(url));
        }

        private async Task UpdateContentAsync()
        {
            // Pre-fetch libraries
            List<Task> tasks = new(3) { PrefetchMusicLibraryAsync(), PrefetchVideosLibraryAsync() };

            // Update recent media
            if (_settingsService.ShowRecent)
            {
                tasks.Add(UpdateRecentMediaListAsync(true));
            }
            else
            {
                Recent.Clear();
            }

            // Await for all of them
            await Task.WhenAll(tasks);
        }

        private async Task PrefetchMusicLibraryAsync()
        {
            try
            {
                await _libraryService.FetchMusicAsync();
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Music));
            }
        }

        private async Task PrefetchVideosLibraryAsync()
        {
            try
            {
                await _libraryService.FetchVideosAsync();
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Videos));
            }
        }

        private async Task UpdateRecentMediaListAsync(bool loadMediaDetails)
        {
            string[] tokens = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderByDescending(x => x.Metadata)
                .Select(x => x.Token)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (tokens.Length == 0)
            {
                Recent.Clear();
                return;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                StorageFile? file = await ConvertMruTokenToStorageFileAsync(token);
                if (file == null)
                {
                    try
                    {
                        StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
                    }
                    catch (Exception e)
                    {
                        LogService.Log(e);
                    }
                    continue;
                }

                // TODO: Add support for playing playlist file from home page
                if (file.IsSupportedPlaylist()) continue;
                if (!_dispatcher.HasThreadAccess)
                    throw new InvalidOperationException("This method must be called on the UI thread.");

                if (i >= Recent.Count)
                {
                    MediaViewModel media = _mediaFactory.GetSingleton(file);
                    _pathToMruMappings[media.Location] = token;
                    Recent.Add(media);
                }
                else if (Recent[i].Source is StorageFile existing)
                {
                    try
                    {
                        if (!file.IsEqual(existing)) MoveOrInsert(file, token, i);
                    }
                    catch (Exception)
                    {
                        // StorageFile.IsEqual() throws an exception
                        // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
                        // pass
                    }
                }
            }

            // Remove stale items
            while (Recent.Count > tokens.Length)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }

            // Load media details for the remaining items
            if (!loadMediaDetails) return;
            IEnumerable<Task> loadingTasks = Recent.Select(x => x.LoadDetailsAsync(_filesService));
            loadingTasks = Recent.Select(x => x.LoadThumbnailAsync(_filesService)).Concat(loadingTasks);
            await Task.WhenAll(loadingTasks);
        }

        private void MoveOrInsert(StorageFile file, string token, int desiredIndex)
        {
            // Find index of the VM of the same file
            // There is no FindIndex method for ObservableCollection :(
            int existingIndex = -1;
            for (int j = desiredIndex + 1; j < Recent.Count; j++)
            {
                if (Recent[j].Source is StorageFile existingFile && file.IsEqual(existingFile))
                {
                    existingIndex = j;
                    break;
                }
            }

            if (existingIndex == -1)
            {
                MediaViewModel media = _mediaFactory.GetSingleton(file);
                _pathToMruMappings[media.Location] = token;
                Recent.Insert(desiredIndex, media);
            }
            else
            {
                MediaViewModel toInsert = Recent[existingIndex];
                Recent.RemoveAt(existingIndex);
                Recent.Insert(desiredIndex, toInsert);
            }
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (media.IsMediaActive)
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                Messenger.Send(new PlayMediaMessage(media, false));
            }
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }

        [RelayCommand]
        private void Remove(MediaViewModel media)
        {
            Recent.Remove(media);
            if (_pathToMruMappings.Remove(media.Location, out string token))
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            }
        }

        [RelayCommand]
        private async Task OpenFolderAsync()
        {
            StorageFolder? folder = await _filesService.PickFolderAsync();
            if (folder == null) return;
            IReadOnlyList<IStorageItem> items = await _filesService.GetSupportedItems(folder).GetItemsAsync();
            IStorageFile[] files = items.OfType<IStorageFile>().ToArray();
            if (files.Length == 0) return;
            Messenger.Send(new PlayMediaMessage(files));
        }

        private static async Task<StorageFile?> ConvertMruTokenToStorageFileAsync(string token)
        {
            try
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}