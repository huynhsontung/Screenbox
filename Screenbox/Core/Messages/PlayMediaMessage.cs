#nullable enable

using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Search;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal class PlayMediaMessage : ValueChangedMessage<object>
    {
        public StorageFileQueryResult? NeighboringFilesQuery { get; }

        public PlayMediaMessage(object value) : base(value)
        {
        }

        public PlayMediaMessage(IReadOnlyList<IStorageItem> files, StorageFileQueryResult? neighboringFilesQuery) :
            base(files)
        {
            NeighboringFilesQuery = neighboringFilesQuery;
        }
    }
}
