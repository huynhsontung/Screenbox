#nullable enable

using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Search;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class PlayFilesWithNeighborsMessage : ValueChangedMessage<IReadOnlyList<IStorageItem>>
    {
        public StorageFileQueryResult NeighboringFilesQuery { get; }

        public PlayFilesWithNeighborsMessage(IReadOnlyList<IStorageItem> files,
            StorageFileQueryResult neighboringFilesQuery) : base(files)
        {
            NeighboringFilesQuery = neighboringFilesQuery;
        }
    }
}
