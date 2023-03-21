#nullable enable

using System;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class NotificationViewModel : ObservableRecipient,
        IRecipient<RaiseFrameSavedNotificationMessage>,
        IRecipient<RaiseResumePositionNotificationMessage>,
        IRecipient<CloseNotificationMessage>,
        IRecipient<ErrorMessage>
    {
        [ObservableProperty] private NotificationLevel _severity;

        [ObservableProperty] private string? _title;

        [ObservableProperty] private string? _message;

        [ObservableProperty] private object? _content;

        [ObservableProperty] private bool _isOpen;

        [ObservableProperty] private ButtonBase? _actionButton;

        public string? ButtonContent { get; private set; }

        public RelayCommand? ActionCommand { get; private set; }

        private readonly INotificationService _notificationService;
        private readonly IFilesService _filesService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;

        public NotificationViewModel(INotificationService notificationService, IFilesService filesService)
        {
            _notificationService = notificationService;
            _filesService = filesService;
            _notificationService.NotificationRaised += NotificationServiceOnNotificationRaised;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(ErrorMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = message.Title;
                Message = message.Message;
                Severity = NotificationLevel.Error;

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(CloseNotificationMessage message)
        {
            IsOpen = false;
        }

        public void Receive(RaiseFrameSavedNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = ResourceHelper.GetString("FrameSavedNotificationTitle");
                Severity = NotificationLevel.Success;
                ButtonContent = message.Value.Name;
                ActionCommand = new RelayCommand(() => _filesService.OpenFileLocationAsync(message.Value));

                ActionButton = new HyperlinkButton
                {
                    Content = ButtonContent,
                    Command = ActionCommand
                };
                
                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(8));
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(RaiseResumePositionNotificationMessage message)
        {
            if (Severity == NotificationLevel.Error && IsOpen) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                if (message.Value <= TimeSpan.Zero) return;
                Title = ResourceHelper.GetString("ResumePositionNotificationTitle");
                Severity = NotificationLevel.Info;
                ButtonContent = ResourceHelper.GetString("GoToPosition", Humanizer.ToDuration(message.Value));
                ActionCommand = new RelayCommand(() =>
                {
                    IsOpen = false;
                    Messenger.Send(new ChangeTimeRequestMessage(message.Value, debounce: false));
                });

                ActionButton = new Button
                {
                    Content = ButtonContent,
                    Command = ActionCommand
                };

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        [RelayCommand]
        private void Close()
        {
            // IsOpen = false;
            Messenger.Send<CloseNotificationMessage>();
        }

        private void Reset()
        {
            Title = default;
            Message = default;
            Severity = default;
            ButtonContent = default;
            ActionCommand = default;
            Content = default;
            ActionButton = default;
            IsOpen = false;
        }

        private void NotificationServiceOnNotificationRaised(object sender, NotificationRaisedEventArgs e)
        {
            void SetNotification()
            {
                Reset();
                Title = e.Title;
                Message = e.Message;
                Severity = e.Level;

                TimeSpan timeout;
                switch (e.Level)
                {
                    case NotificationLevel.Warning:
                        timeout = TimeSpan.FromSeconds(10);
                        break;
                    case NotificationLevel.Error:
                        timeout = TimeSpan.FromSeconds(15);
                        break;
                    default:
                        timeout = TimeSpan.FromSeconds(6);
                        break;
                }

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, timeout);
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }
    }
}
