#nullable enable

using System;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Converters;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal sealed partial class NotificationViewModel : ObservableRecipient,
        IRecipient<RaiseFrameSavedNotificationMessage>,
        IRecipient<RaiseResumePositionNotificationMessage>,
        IRecipient<ErrorMessage>
    {
        [ObservableProperty] private InfoBarSeverity _severity;

        [ObservableProperty] private string? _title;

        [ObservableProperty] private string? _message;

        [ObservableProperty] private object? _content;

        [ObservableProperty] private string? _buttonContent;

        [ObservableProperty] private RelayCommand? _actionCommand;

        [ObservableProperty] private bool _isOpen;

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
                Severity = InfoBarSeverity.Error;

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(RaiseFrameSavedNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = Resources.FrameSavedNotificationTitle;
                Severity = InfoBarSeverity.Success;
                ButtonContent = message.Value.Name;
                ActionCommand = new RelayCommand(() => _filesService.OpenFileLocationAsync(message.Value));
                
                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(8));
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(RaiseResumePositionNotificationMessage message)
        {
            if (Severity == InfoBarSeverity.Error && IsOpen) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                if (message.Value <= TimeSpan.Zero) return;
                Title = Resources.ResumePositionNotificationTitle;
                Severity = InfoBarSeverity.Informational;
                ButtonContent = Resources.GoToPosition(HumanizedDurationConverter.Convert(message.Value));
                ActionCommand = new RelayCommand(() =>
                {
                    IsOpen = false;
                    Messenger.Send(new ChangeTimeRequestMessage(message.Value, debounce: false));
                });

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        private void Reset()
        {
            Title = default;
            Message = default;
            Severity = default;
            ButtonContent = default;
            ActionCommand = default;
            Content = default;
            IsOpen = false;
        }

        private void NotificationServiceOnNotificationRaised(object sender, NotificationRaisedEventArgs e)
        {
            void SetNotification()
            {
                Reset();
                Title = e.Title;
                Message = e.Message;
                Severity = ConvertInfoBarSeverity(e.Level);

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

        private InfoBarSeverity ConvertInfoBarSeverity(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return InfoBarSeverity.Error;
                case NotificationLevel.Warning:
                    return InfoBarSeverity.Warning;
                case NotificationLevel.Success:
                    return InfoBarSeverity.Success;
                default:
                    return InfoBarSeverity.Informational;
            }
        }
    }
}
