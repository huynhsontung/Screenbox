#nullable enable

using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.Messages;
using Screenbox.Services;
using Screenbox.Strings;

namespace Screenbox.ViewModels
{
    internal partial class NotificationViewModel : ObservableRecipient, IRecipient<RaiseFrameSavedNotificationMessage>, IRecipient<ErrorMessage>
    {
        [ObservableProperty] private InfoBarSeverity _severity;

        [ObservableProperty] private string? _title;

        [ObservableProperty] private string? _message;

        [ObservableProperty] private object? _content;

        [ObservableProperty] private ButtonBase? _actionButton;

        [ObservableProperty] private bool _isOpen;

        private readonly INotificationService _notificationService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;

        public NotificationViewModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
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
                ActionButton = new HyperlinkButton
                {
                    Content = message.Value.Name,
                };

                ActionButton.Click += (_, _) => OpenSaveFolder(message.Value);

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(8));
            }

            _dispatcherQueue.TryEnqueue(SetNotification);
        }

        private void Reset()
        {
            Title = default;
            Message = default;
            Severity = default;
            ActionButton = default;
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

        private async void OpenSaveFolder(StorageFile savedFrame)
        {
            StorageFolder? saveFolder = await savedFrame.GetParentAsync();
            FolderLauncherOptions options = new();
            options.ItemsToSelect.Add(savedFrame);
            await Launcher.LaunchFolderAsync(saveFolder, options);
        }
    }
}
