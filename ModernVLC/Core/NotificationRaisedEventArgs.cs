using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernVLC.Core
{
    internal enum NotificationLevel
    {
        Info,
        Warning,
        Error,
    }

    internal class NotificationRaisedEventArgs : EventArgs
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public NotificationLevel Level { get; set; }

        public object Content { get; set; }
    }
}
