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

    internal sealed class NotificationRaisedEventArgs : EventArgs
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public NotificationLevel Level { get; set; }

        public NotificationRaisedEventArgs(NotificationLevel level, string title, string message)
        {
            Level = level;
            Title = title;
            Message = message;
        }
    }
}
