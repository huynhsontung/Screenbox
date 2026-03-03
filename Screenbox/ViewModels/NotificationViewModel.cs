#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Screenbox.Strings;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.ViewModels
{
    public sealed partial class NotificationViewModel : ObservableRecipient,
        IRecipient<RaiseFrameSavedNotificationMessage>,
        IRecipient<RaiseResumePositionNotificationMessage>,
        IRecipient<RaiseLibraryAccessDeniedNotificationMessage>,
        IRecipient<MediaLoadFailedNotificationMessage>,
        IRecipient<CloseNotificationMessage>,
        IRecipient<SubtitleAddedNotificationMessage>,
        IRecipient<RaiseNotificationMessage>,
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

        private readonly IFilesService _filesService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _timer;

        public NotificationViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();

            // Activate the view model's messenger
            IsActive = true;
        }

        /// <summary>
        /// Handles a general error message.
        /// </summary>
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

        /// <summary>
        /// Handles a request to close the notification.
        /// </summary>
        public void Receive(CloseNotificationMessage message)
        {
            IsOpen = false;
        }

        /// <summary>
        /// Handles a notification raised via the messaging pattern (e.g., from VLC dialog handlers).
        /// </summary>
        public void Receive(RaiseNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = message.Title;
                Message = message.Message;
                Severity = message.Level;

                TimeSpan timeout;
                switch (message.Level)
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

        /// <summary>
        /// Handles a notification indicating a subtitle was added.
        /// </summary>
        public void Receive(SubtitleAddedNotificationMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = Resources.SubtitleAddedNotificationTitle;
                Severity = NotificationLevel.Success;
                Message = message.File.Name;

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(5));
            });
        }

        /// <summary>
        /// Handles a notification indicating media failed to load.
        /// </summary>
        public void Receive(MediaLoadFailedNotificationMessage message)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = Resources.FailedToLoadMediaNotificationTitle;
                Severity = NotificationLevel.Error;
                Message = string.IsNullOrEmpty(message.Reason) || string.IsNullOrEmpty(message.Path)
                    ? $"{message.Path}{message.Reason}"
                    : $"{message.Path}{Environment.NewLine}{message.Reason}";

                IsOpen = true;
                _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        /// <summary>
        /// Handles a notification indicating a frame was saved.
        /// </summary>
        public void Receive(RaiseFrameSavedNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = Resources.FrameSavedNotificationTitle;
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

        /// <summary>
        /// Handles a notification to resume media at a previous position.
        /// </summary>
        public void Receive(RaiseResumePositionNotificationMessage message)
        {
            if (Severity == NotificationLevel.Error && IsOpen) return;
            _dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                if (message.Value <= TimeSpan.Zero) return;
                Title = Resources.ResumePositionNotificationTitle;
                Severity = NotificationLevel.Info;
                ButtonContent = Resources.GoToPosition(Humanizer.ToDuration(message.Value));
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

        /// <summary>
        /// Handles a notification indicating library access was denied.
        /// </summary>
        public void Receive(RaiseLibraryAccessDeniedNotificationMessage message)
        {
            string title;
            Uri link;
            switch (message.Library)
            {
                case KnownLibraryId.Music:
                    title = Resources.AccessDeniedMusicLibraryTitle;
                    link = new Uri("ms-settings:privacy-musiclibrary");
                    break;
                case KnownLibraryId.Pictures:
                    title = Resources.AccessDeniedPicturesLibraryTitle;
                    link = new Uri("ms-settings:privacy-pictures");
                    break;
                case KnownLibraryId.Videos:
                    title = Resources.AccessDeniedVideosLibraryTitle;
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
                ButtonContent = Resources.OpenPrivacySettingsButtonText;
                Message = Resources.AccessDeniedMessage;
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
    }
}
