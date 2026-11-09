using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class NotificationViewModel : ObservableRecipient,
    IRecipient<NotificationMessage>,
    IRecipient<CloseNotificationMessage>,
    IRecipient<QueueCurrentItemChangedMessage>
{
    [ObservableProperty]
    public partial NotificationKind Kind { get; set; }

    [ObservableProperty]
    public partial NotificationLevel Severity { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial double? NumericValue { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial string? ActionContent { get; set; }

    [ObservableProperty]
    public partial ICommand? ActionCommand { get; set; }

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _timer;
    private readonly Queue<NotificationMessage> _pendingNotifications = new();

    public NotificationViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();

        Messenger.Register<NotificationMessage>(this);
        Messenger.Register<CloseNotificationMessage>(this);
        Messenger.Register<QueueCurrentItemChangedMessage>(this);
    }

    /// <summary>
    /// Handles an incoming application notification message.
    /// </summary>
    /// <param name="message">The notification message to display or queue.</param>
    public void Receive(NotificationMessage message)
    {
        if (message.Kind is NotificationKind.ResumePosition or NotificationKind.EnqueueFolderItems &&
            Severity is NotificationLevel.Error &&
            IsOpen)
        {
            return;
        }

        _dispatcherQueue.TryExecuteOrEnqueue(() =>
        {
            if (IsOpen)
            {
                if (message.Level is NotificationLevel.Error)
                {
                    _pendingNotifications.Clear();
                    DisplayNotification(message);
                    return;
                }

                if (Severity is NotificationLevel.Error)
                {
                    return;
                }

                // Avoid queuing duplicate interactive prompts of the same kind
                if (message.Kind is NotificationKind.ResumePosition or NotificationKind.EnqueueFolderItems)
                {
                    var filtered = _pendingNotifications
                        .Where(n => n.Kind != message.Kind)
                        .ToList();
                    _pendingNotifications.Clear();
                    foreach (var item in filtered)
                    {
                        _pendingNotifications.Enqueue(item);
                    }
                }

                _pendingNotifications.Enqueue(message);
            }
            else
            {
                DisplayNotification(message);
            }
        });
    }

    /// <summary>
    /// Handles a request to close the current notification and show the next queued notification if present.
    /// </summary>
    /// <param name="message">The close notification message.</param>
    public void Receive(CloseNotificationMessage message)
    {
        _dispatcherQueue.TryExecuteOrEnqueue(DismissCurrentAndShowNext);
    }

    /// <summary>
    /// Handles a change in the active play queue item by clearing stale media-specific notifications.
    /// </summary>
    /// <param name="message">The message containing the new current queue item.</param>
    public void Receive(QueueCurrentItemChangedMessage message)
    {
        _dispatcherQueue.TryExecuteOrEnqueue(() =>
        {
            if (_pendingNotifications.Count > 0)
            {
                var remaining = _pendingNotifications
                    .Where(n => n.Kind is not NotificationKind.ResumePosition and not NotificationKind.EnqueueFolderItems)
                    .ToList();
                _pendingNotifications.Clear();
                foreach (var item in remaining)
                {
                    _pendingNotifications.Enqueue(item);
                }
            }

            if (Kind is NotificationKind.ResumePosition or NotificationKind.EnqueueFolderItems)
            {
                DismissCurrentAndShowNext();
            }
        });
    }

    [RelayCommand]
    private void Close()
    {
        Messenger.Send<CloseNotificationMessage>();
    }

    private void DisplayNotification(NotificationMessage message)
    {
        Reset();
        Severity = message.Level;
        Kind = message.Kind;

        if (message.Title is not null)
            Title = message.Title;

        if (message.Message is not null)
            Message = message.Message;

        if (message.NumericValue.HasValue)
            NumericValue = message.NumericValue;

        if (message.ActionContent is not null)
            ActionContent = message.ActionContent;

        if (message.ActionCommand is not null)
            ActionCommand = message.ActionCommand;

        IsOpen = true;
        TimeSpan duration = message.DurationOverride ?? GetNotificationDuration(message.Level);
        _timer.Debounce(DismissCurrentAndShowNext, duration);
    }

    private void DismissCurrentAndShowNext()
    {
        _timer.Stop();
        if (_pendingNotifications.TryDequeue(out var nextMessage))
        {
            DisplayNotification(nextMessage);
        }
        else
        {
            Reset();
        }
    }

    private void Reset()
    {
        _timer.Stop();
        Kind = NotificationKind.None;
        Severity = default;
        Title = default;
        NumericValue = default;
        Message = default;
        ActionContent = default;
        ActionCommand = default;
        IsOpen = false;
    }

    private static TimeSpan GetNotificationDuration(NotificationLevel level)
    {
        return level switch
        {
            NotificationLevel.Error => TimeSpan.FromSeconds(15.0),
            NotificationLevel.Warning => TimeSpan.FromSeconds(8.0),
            NotificationLevel.Info => TimeSpan.FromSeconds(5.0),
            NotificationLevel.Success => TimeSpan.FromSeconds(5.0),
            _ => TimeSpan.FromSeconds(8.0),
        };
    }
}
