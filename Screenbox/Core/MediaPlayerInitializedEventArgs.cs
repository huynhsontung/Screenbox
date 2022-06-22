using Screenbox.Core.Playback;
using System;

namespace Screenbox.Core
{
    internal class MediaPlayerInitializedEventArgs : EventArgs
    {
        public IMediaPlayer MediaPlayer { get; }

        public MediaPlayerInitializedEventArgs(IMediaPlayer player)
        {
            MediaPlayer = player;
        }
    }
}
