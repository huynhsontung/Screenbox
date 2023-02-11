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

        private readonly StorageLibraryChangeReader _changeReader;

        public StorageLibraryChangeResult(StorageLibraryChangeReader changeReader, List<StorageFile> addedItems, List<string> removedItems)
        {
            _changeReader = changeReader;
            AddedItems = addedItems;
            RemovedItems = removedItems;
            ulong lastChangeId = changeReader.GetLastChangeId();
            Status = StorageLibraryChangeStatus.NoChange;
            if (lastChangeId == StorageLibraryLastChangeId.Unknown)
            {
                Status = StorageLibraryChangeStatus.Unknown;
            }
            else if (lastChangeId > 0)
            {
                Status = StorageLibraryChangeStatus.HasChange;
            }
        }

        public StorageLibraryChangeResult(StorageLibraryChangeStatus status, StorageLibraryChangeReader changeReader)
        {
            _changeReader = changeReader;
            AddedItems = new List<StorageFile>();
            RemovedItems = new List<string>();
            Status = status;
        }

        public async void Dispose()
        {
            try
            {
                await _changeReader.AcceptChangesAsync();
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
