using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.ViewModels;
using Windows.Storage.Search;

namespace Screenbox.Core.Messages;

/// <summary>
/// Sent when the currently active item in the play queue changes.
/// </summary>
public sealed class QueueCurrentItemChangedMessage : ValueChangedMessage<MediaViewModel?>
{
    public StorageFileQueryResult? NeighboringFilesQuery { get; }

    public QueueCurrentItemChangedMessage(MediaViewModel? value) : base(value)
    {
    }

    public QueueCurrentItemChangedMessage(MediaViewModel? value, StorageFileQueryResult? neighboringFilesQuery) : base(value)
    {
        NeighboringFilesQuery = neighboringFilesQuery;
    }
}
