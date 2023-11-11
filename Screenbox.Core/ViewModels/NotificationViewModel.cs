#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class NotificationViewModel : ObservableRecipient,
        IRecipient<RaiseFrameSavedNotificationMessage>,
        IRecipient<RaiseResumePositionNotificationMessage>,
        IRecipient<RaiseLibraryAccessDeniedNotificationMessage>,
        IRecipient<MediaLoadFailedNotificationMessage>,
        IRecipient<CloseNotificationMessage>,
        IRecipient<SubtitleAddedNotificationMessage>,
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
        private readonly IResourceService _resourceService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;

        public NotificationViewModel(INotificationService notificationService, IFilesService filesService,
            IResourceService resourceService)
        {
            _notificationService = notificationService;
            _filesService = filesService;
            _resourceService = resourceService;
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

        public void Receive(SubtitleAddedNotificationMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = _resourceService.GetString(ResourceName.SubtitleAddedNotificationTitle);
                Severity = NotificationLevel.Success;
                Message = message.File.Name;

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(5));
            });
        }

        public void Receive(MediaLoadFailedNotificationMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = _resourceService.GetString(ResourceName.FailedToLoadMediaNotificationTitle);
                Severity = NotificationLevel.Error;
                Message = string.IsNullOrEmpty(message.Reason) || string.IsNullOrEmpty(message.Path)
                    ? $"{message.Path}{message.Reason}"
                    : $"{message.Path}{Environment.NewLine}{message.Reason}";

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        public void Receive(RaiseFrameSavedNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = _resourceService.GetString(ResourceName.FrameSavedNotificationTitle);
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
                Title = _resourceService.GetString(ResourceName.ResumePositionNotificationTitle);
                Severity = NotificationLevel.Info;
                ButtonContent = _resourceService.GetString(ResourceName.GoToPosition, Humanizer.ToDuration(message.Value));
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

        public void Receive(RaiseLibraryAccessDeniedNotificationMessage message)
        {
            string title;
            Uri link;
            switch (message.Library)
            {
                case KnownLibraryId.Music:
                    title = _resourceService.GetString(ResourceName.AccessDeniedMusicLibraryTitle);
                    link = new Uri("ms-settings:privacy-musiclibrary");
                    break;
                case KnownLibraryId.Pictures:
                    title = _resourceService.GetString(ResourceName.AccessDeniedPicturesLibraryTitle);
                    link = new Uri("ms-settings:privacy-pictures");
                    break;
                case KnownLibraryId.Videos:
                    title = _resourceService.GetString(ResourceName.AccessDeniedVideosLibraryTitle);
                    link = new Uri("ms-settings:privacy-videos");
                    break;
                case KnownLibraryId.Documents:
                default:
                    return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = title;
                Severity = NotificationLevel.Error;
                ButtonContent = _resourceService.GetString(ResourceName.OpenPrivacySettingsButtonText);
                Message = _resourceService.GetString(ResourceName.AccessDeniedMessage);
                ActionCommand = new RelayCommand(() =>
                {
                    IsOpen = false;
                    _ = Launcher.LaunchUriAsync(link);
                });

                ActionButton = new HyperlinkButton
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
