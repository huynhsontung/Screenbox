#nullable enable

using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Screenbox.Core
{
    internal enum StorageLibraryChangeStatus
    {
        NoChange,
        HasChange,
        Unknown
    }

    internal sealed class StorageLibraryChangeResult : IDisposable
    {
        public StorageLibraryChangeStatus Status { get; }
        public List<StorageFile> AddedItems { get; }
        public List<string> RemovedItems { get; }

        private readonly StorageLibraryChangeReader? _changeReader;

        public StorageLibraryChangeResult(StorageLibraryChangeReader changeReader, List<StorageFile> addedItems, List<string> removedItems)
        {
            _changeReader = changeReader;
            AddedItems = addedItems;
            RemovedItems = removedItems;
            Status = addedItems.Count > 0 || removedItems.Count > 0
                ? StorageLibraryChangeStatus.HasChange
                : StorageLibraryChangeStatus.NoChange;
        }

        public StorageLibraryChangeResult(StorageLibraryChangeStatus status)
        {
            AddedItems = new List<StorageFile>();
            RemovedItems = new List<string>();
            Status = status;
        }

        public async void Dispose()
        {
            try
            {
                await _changeReader?.AcceptChangesAsync();
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
