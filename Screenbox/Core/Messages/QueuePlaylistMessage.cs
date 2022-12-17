using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.ViewModels;

namespace Screenbox.Core.Messages
{
    internal sealed class QueuePlaylistMessage : ValueChangedMessage<IEnumerable<MediaViewModel>>
    {
        public bool AddNext { get; }

        public QueuePlaylistMessage(MediaViewModel target, bool addNext = false) : base(new[] { target })
        {
            AddNext = addNext;
        }

        public QueuePlaylistMessage(IList<MediaViewModel> playlist, bool addNext = false) : base(playlist)
        {
            AddNext = addNext;
        }
    }
}
