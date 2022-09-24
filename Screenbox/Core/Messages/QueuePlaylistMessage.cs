using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.ViewModels;

namespace Screenbox.Core.Messages
{
    internal sealed class QueuePlaylistMessage : ValueChangedMessage<IEnumerable<MediaViewModel>>
    {
        public int StartIndex { get; }

        public MediaViewModel Target { get; }

        public QueuePlaylistMessage(IList<MediaViewModel> playlist, MediaViewModel target) : base(playlist)
        {
            Target = target;
            StartIndex = playlist.IndexOf(Target);
        }

        public QueuePlaylistMessage(IList<MediaViewModel> playlist, int startIndex) : base(playlist)
        {
            Target = playlist[startIndex];
            StartIndex = startIndex;
        }
    }
}
