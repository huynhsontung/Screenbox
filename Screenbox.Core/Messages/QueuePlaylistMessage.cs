﻿using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;

using MediaViewModel = Screenbox.Core.ViewModels.MediaViewModel;

namespace Screenbox.Core.Messages
{
    public sealed class QueuePlaylistMessage : ValueChangedMessage<IEnumerable<MediaViewModel>>
    {
        public bool AddNext { get; }

        public QueuePlaylistMessage(IEnumerable<MediaViewModel> playlist, bool addNext = false) : base(playlist)
        {
            AddNext = addNext;
        }
    }
}
