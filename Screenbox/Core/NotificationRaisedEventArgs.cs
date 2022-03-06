#nullable enable

using System;

namespace Screenbox.Core
{
    internal enum NotificationLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    internal class NotificationRaisedEventArgs : EventArgs
    {
        public string? Title { get; set; }

        public string? Message { get; set; }

        public NotificationLevel Level { get; set; }

        public object? Content { get; set; }

        public Uri? Link { get; set; }

        public string? LinkText { get; set; }
    }
}
