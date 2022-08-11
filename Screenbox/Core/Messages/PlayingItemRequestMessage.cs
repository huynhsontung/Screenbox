#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.ViewModels;

namespace Screenbox.Core.Messages
{
    internal class PlayingItemRequestMessage : RequestMessage<MediaViewModel?>
    {
    }
}
