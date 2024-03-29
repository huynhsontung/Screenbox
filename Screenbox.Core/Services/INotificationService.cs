﻿#nullable enable

using System;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;

namespace Screenbox.Core.Services
{
    public interface INotificationService
    {
        event EventHandler<NotificationRaisedEventArgs> NotificationRaised;
        event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        void RaiseError(string title, string message);
        void RaiseInfo(string title, string message);
        void RaiseNotification(NotificationLevel level, string title, string message);
        void RaiseWarning(string title, string message);
    }
}