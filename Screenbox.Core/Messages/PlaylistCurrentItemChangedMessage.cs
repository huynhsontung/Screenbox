#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Messages
{
    public sealed class PlaylistCurrentItemChangedMessage : ValueChangedMessage<MediaViewModel?>
    {
        public PlaylistCurrentItemChangedMessage(MediaViewModel? value) : base(value)
        {
        }
    }
}
