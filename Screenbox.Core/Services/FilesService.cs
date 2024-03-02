﻿#nullable enable

using ProtoBuf;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.Services
{
    public sealed class FilesService : IFilesService
    {
        public async Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null)
        {
            try
            {
                StorageFolder? parent = await file.GetParentAsync();
                options ??= new QueryOptions(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
                StorageFileQueryResult? queryResult = parent?.CreateFileQueryWithOptions(options);
                return queryResult;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery)
        {
            // Due to limitations with NeighboringFilesQuery, manually find the next supported file
            uint startIndex = await neighboringFilesQuery.FindStartIndexAsync(currentFile);
            if (startIndex == uint.MaxValue) return null;
            startIndex += 1;

            // The following line return a native vector view.
            // It does not fetch all the files in the directory at once.
            // No need for manual paging!
            IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(startIndex, uint.MaxValue);
            return files.FirstOrDefault(x => x.IsSupported());
        }

        public async Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile, StorageFileQueryResult neighboringFilesQuery)
        {
            // Due to limitations with NeighboringFilesQuery, manually find the previous supported file
            uint startIndex = await neighboringFilesQuery.FindStartIndexAsync(currentFile);
            if (startIndex == uint.MaxValue) return null;

            // The following line return a native vector view.
            // It does not fetch all the files in the directory at once.
            // No need for manual paging!
            IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(0, startIndex);
            return files.LastOrDefault(x => x.IsSupported());
        }

        public async Task<StorageItemThumbnail?> GetThumbnailAsync(StorageFile file, bool allowIcon = false)
        {
            if (!file.IsAvailable) return null;
            try
            {
                StorageItemThumbnail? source = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                if (source != null && (source.Type == ThumbnailType.Image || allowIcon))
                {
                    return source;
                }
            }
            catch (Exception e)
            {
                // System.Exception: The data necessary to complete this operation is not yet available.
                if (e.HResult != unchecked((int)0x8000000A) &&
                    // System.Exception: The RPC server is unavailable.
                    e.HResult != unchecked((int)0x800706BA))
                    LogService.Log(e);
            }

            return null;
        }

        public StorageItemQueryResult GetSupportedItems(StorageFolder folder)
        {
            // Don't use indexer when querying. Potential incomplete result.
            QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
            return folder.CreateItemQueryWithOptions(queryOptions);
        }

        public IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder)
        {
            QueryOptions queryOptions = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedFormats);
            return folder.CreateItemQueryWithOptions(queryOptions).GetItemCountAsync();
        }

        public IAsyncOperation<StorageFile> PickFileAsync(params string[] formats)
        {
            FileOpenPicker picker = GetFilePickerForFormats(formats);
            return picker.PickSingleFileAsync();
        }

        public IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(params string[] formats)
        {
            FileOpenPicker picker = GetFilePickerForFormats(formats);
            return picker.PickMultipleFilesAsync();
        }

        public IAsyncOperation<StorageFolder> PickFolderAsync()
        {
            FolderPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };

            foreach (string supportedFormat in FilesHelpers.SupportedFormats)
            {
                picker.FileTypeFilter.Add(supportedFormat);
            }

            return picker.PickSingleFolderAsync();
        }

        public async Task<StorageFile> SaveToDiskAsync<T>(StorageFolder folder, string fileName, T source)
        {
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await SaveToDiskAsync(file, source);
            return file;
        }

        public async Task SaveToDiskAsync<T>(StorageFile file, T source)
        {
            using Stream stream = await file.OpenStreamForWriteAsync();
            Serializer.Serialize(stream, source);
            stream.SetLength(stream.Position);  // A weird quirk of protobuf-net
            await stream.FlushAsync();
        }

        public async Task<T> LoadFromDiskAsync<T>(StorageFolder folder, string fileName)
        {
            StorageFile file = await folder.GetFileAsync(fileName);
            return await LoadFromDiskAsync<T>(file);
        }

        public async Task<T> LoadFromDiskAsync<T>(StorageFile file)
        {
            using Stream readStream = await file.OpenStreamForReadAsync();
            return Serializer.Deserialize<T>(readStream);
        }

        public async Task OpenFileLocationAsync(string path)
        {
            string? folderPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folderPath))
                await Launcher.LaunchFolderPathAsync(folderPath);
        }

        public async Task OpenFileLocationAsync(StorageFile file)
        {
            StorageFolder? folder = await file.GetParentAsync();
            if (folder == null)
            {
                await OpenFileLocationAsync(file.Path);
            }
            else
            {
                FolderLauncherOptions options = new();
                options.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderAsync(folder, options);
            }
        }

        public void AddToRecent(IStorageItem item)
        {
            string metadata = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(item, metadata);
            }
            catch (Exception)
            {
                // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
                // Ownership issue?
            }
        }

        public async Task<MediaInfo> GetMediaInfoAsync(StorageFile file)
        {
            if (!file.IsAvailable) return new MediaInfo();

            try
            {
                BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
                MediaPlaybackType mediaType = GetMediaTypeForFile(file);
                switch (mediaType)
                {
                    case MediaPlaybackType.Video:
                        VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                        return new MediaInfo(basicProperties, videoProperties);
                    case MediaPlaybackType.Music:
                        MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
                        return new MediaInfo(basicProperties, musicProperties);
                }
            }
            catch (Exception e)
            {
                // System.Exception: The RPC server is unavailable.
                if (e.HResult != unchecked((int)0x800706BA))
                    LogService.Log(e);
            }

            return new MediaInfo();
        }

        private FileOpenPicker GetFilePickerForFormats(IReadOnlyCollection<string> formats)
        {
            FileOpenPicker picker = new()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            IEnumerable<string> fileTypes = formats;
            if (formats.Count == 0)
            {
                fileTypes = FilesHelpers.SupportedFormats;
                picker.FileTypeFilter.Add("*");
            }

            foreach (string? fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            return picker;
        }

        private static MediaPlaybackType GetMediaTypeForFile(IStorageFile file)
        {
            if (file.IsSupportedVideo()) return MediaPlaybackType.Video;
            if (file.IsSupportedAudio()) return MediaPlaybackType.Music;
            if (file.ContentType.StartsWith("image")) return MediaPlaybackType.Image;
            if (file.IsSupportedPlaylist()) return MediaPlaybackType.Playlist;
            return MediaPlaybackType.Unknown;
        }
    }
}
