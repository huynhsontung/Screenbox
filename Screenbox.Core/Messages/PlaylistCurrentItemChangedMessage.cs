#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.Storage.Search;

using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Messages
{
    public sealed class PlaylistCurrentItemChangedMessage : ValueChangedMessage<MediaViewModel?>
    {
        public StorageFileQueryResult? NeighboringFilesQuery { get; }

        public PlaylistCurrentItemChangedMessage(MediaViewModel? value) : base(value)
        {
        }

        public PlaylistCurrentItemChangedMessage(MediaViewModel? value, StorageFileQueryResult? neighboringFilesQuery) : base(value)
        {
            NeighboringFilesQuery = neighboringFilesQuery;
        }
    }
}
