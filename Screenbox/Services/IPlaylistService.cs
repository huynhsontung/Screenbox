using System;

namespace Screenbox.Services
{
    internal interface IPlaylistService
    {
        event EventHandler<object> OpenRequested;
        void RequestOpen(object value);
    }
}