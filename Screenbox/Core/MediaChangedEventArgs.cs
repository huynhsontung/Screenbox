using System;

namespace Screenbox.Core
{
    internal class MediaChangedEventArgs : EventArgs
    {
        public MediaHandle Handle { get; }

        internal MediaChangedEventArgs(MediaHandle handle)
        {
            Handle = handle;
        }
    }
}
