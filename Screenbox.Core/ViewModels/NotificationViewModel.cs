using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class NotificationViewModel : ObservableRecipient,
    IRecipient<NotificationMessage>,
    IRecipient<CloseNotificationMessage>
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

    public NotificationViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();

        Messenger.Register<NotificationMessage>(this);
        Messenger.Register<CloseNotificationMessage>(this);
    }

    public void Receive(NotificationMessage message)
    {
        if (message.Kind == NotificationKind.ResumePosition && Severity is NotificationLevel.Error && IsOpen)
            return;

        TimeSpan duration = GetNotificationDuration(message.Level);

        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Severity = message.Level;
            Kind = message.Kind;
            Title = message.Title;
            Message = message.Message;
            NumericValue = message.NumericValue;
            ActionContent = message.ActionContent;
            ActionCommand = message.ActionCommand;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, duration);
        });
    }

    public void Receive(CloseNotificationMessage message)
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void Close()
    {
        Messenger.Send<CloseNotificationMessage>();
    }

    private void Reset()
    {
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
