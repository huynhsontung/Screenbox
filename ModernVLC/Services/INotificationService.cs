using LibVLCSharp.Shared;
using ModernVLC.Core;
using System;

namespace ModernVLC.Services
{
    internal interface INotificationService
    {
        event EventHandler<NotificationRaisedEventArgs> NotificationRaised;
        event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        void RaiseError(string title, string message = null);
        void RaiseInfo(string title, string message = null);
        void RaiseNotification(NotificationLevel level, string title, string message = null, object content = null);
        void RaiseWarning(string title, string message = null);
        void SetVLCDiaglogHandlers(LibVLC libVLC);
    }
}