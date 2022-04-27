#nullable enable

using System;
using LibVLCSharp.Shared;
using Screenbox.Core;

namespace Screenbox.Services
{
    internal interface INotificationService
    {
        event EventHandler<NotificationRaisedEventArgs> NotificationRaised;
        event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        void RaiseError(string? title, string? message);
        void RaiseInfo(string? title, string? message);
        void RaiseNotification(NotificationLevel level, string? title, string? message, object? content = null);
        void RaiseWarning(string? title, string? message);
        void SetVLCDiaglogHandlers(LibVLC libVLC);
    }
}