#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.Messages;

public sealed class PlayFilesMessage : ValueChangedMessage<IReadOnlyList<IStorageItem>>
{
    public StorageFileQueryResult? NeighboringFilesQuery { get; }

    public PlayFilesMessage(IReadOnlyList<IStorageItem> files) : base(files) { }

    public PlayFilesMessage(IReadOnlyList<IStorageItem> files,
        StorageFileQueryResult? neighboringFilesQuery) : base(files)
    {
        NeighboringFilesQuery = neighboringFilesQuery;
    }
}