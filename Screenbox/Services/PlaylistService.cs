#nullable enable

using System;

namespace Screenbox.Services
{
    internal class PlaylistService : IPlaylistService
    {
        public event EventHandler<object>? OpenRequested;

        public void RequestOpen(object value) => OpenRequested?.Invoke(this, value);
    }
}
