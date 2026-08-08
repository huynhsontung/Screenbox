#nullable enable

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class NotificationViewModel : ObservableRecipient,
    IRecipient<RaiseFrameSavedNotificationMessage>,
    IRecipient<RaiseResumePositionNotificationMessage>,
    IRecipient<RaiseLibraryAccessDeniedNotificationMessage>,
    IRecipient<MediaLoadFailedNotificationMessage>,
    IRecipient<CloseNotificationMessage>,
    IRecipient<SubtitleAddedNotificationMessage>,
    IRecipient<ErrorMessage>,
    IRecipient<FailedToSaveFrameNotificationMessage>,
    IRecipient<FailedToLoadSubtitleNotificationMessage>,
    IRecipient<FailedToOpenFilesNotificationMessage>,
    IRecipient<FailedToAddFolderNotificationMessage>,
    IRecipient<FailedToInitializeNotificationMessage>,
    IRecipient<PlaylistCreatedNotificationMessage>,
    IRecipient<PlaylistDeletedNotificationMessage>,
    IRecipient<PlaylistRenamedNotificationMessage>,
    IRecipient<PlaylistItemsAddedNotificationMessage>
{
    private const double NotificationDurationShort = 5.0;
    private const double NotificationDurationMedium = 8.0;
    private const double NotificationDurationLong = 15.0;

    [ObservableProperty]
    public partial NotificationKind Kind { get; set; }

    [ObservableProperty]
    public partial NotificationLevel Severity { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial int Count { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial string? ActionContent { get; set; }

    [ObservableProperty]
    public partial ICommand? ActionCommand { get; set; }

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
        ShowErrorNotification(message.Title, message.Message);
    }

    /// <summary>
    /// Handles a request to close the notification.
    /// </summary>
    public void Receive(CloseNotificationMessage message)
    {
        IsOpen = false;
    }

    /// <summary>
    /// Handles a notification indicating a subtitle was added.
    /// </summary>
    public void Receive(SubtitleAddedNotificationMessage message)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Kind = NotificationKind.SubtitleAdded;
            Severity = NotificationLevel.Success;
            Message = message.File.Name;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationShort));
        });
    }

    /// <summary>
    /// Handles a notification indicating media failed to load.
    /// </summary>
    public void Receive(MediaLoadFailedNotificationMessage message)
    {
        string body = string.IsNullOrEmpty(message.Reason) || string.IsNullOrEmpty(message.Path)
            ? $"{message.Path}{message.Reason}"
            : $"{message.Path}{Environment.NewLine}{message.Reason}";
        ShowErrorNotification(NotificationKind.MediaLoadFailed, body);
    }

    /// <summary>
    /// Handles a notification indicating a frame was saved.
    /// </summary>
    public void Receive(RaiseFrameSavedNotificationMessage message)
    {
        void SetNotification()
        {
            Reset();
            Kind = NotificationKind.FrameSaved;
            Severity = NotificationLevel.Success;
            ActionContent = message.Value.Name;
            ActionCommand = new RelayCommand(() => _filesService.OpenFileLocationAsync(message.Value));

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationMedium));
        }

        _dispatcherQueue.TryEnqueue(SetNotification);
    }

    /// <summary>
    /// Handles a notification to resume media at a previous position.
    /// </summary>
    public void Receive(RaiseResumePositionNotificationMessage message)
    {
        if (Severity is NotificationLevel.Error && IsOpen)
            return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            if (message.Value <= TimeSpan.Zero)
                return;

            Kind = NotificationKind.ResumePosition;
            Severity = NotificationLevel.Info;
            ActionContent = Humanizer.ToDuration(message.Value);
            ActionCommand = new RelayCommand(() =>
            {
                IsOpen = false;
                Messenger.Send(new ChangeTimeRequestMessage(message.Value, debounce: false));
            });

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationLong));
        });
    }

    /// <summary>
    /// Handles a notification indicating library access was denied.
    /// </summary>
    public void Receive(RaiseLibraryAccessDeniedNotificationMessage message)
    {
        NotificationKind kind;
        Uri link;
        switch (message.Library)
        {
            case KnownLibraryId.Music:
                kind = NotificationKind.MusicLibraryAccessDenied;
                link = new Uri("ms-settings:privacy-musiclibrary");
                break;
            case KnownLibraryId.Pictures:
                kind = NotificationKind.PicturesLibraryAccessDenied;
                link = new Uri("ms-settings:privacy-pictures");
                break;
            case KnownLibraryId.Videos:
                kind = NotificationKind.VideosLibraryAccessDenied;
                link = new Uri("ms-settings:privacy-videos");
                break;
            case KnownLibraryId.Documents:
            default:
                return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Kind = kind;
            Severity = NotificationLevel.Error;
            ActionCommand = new RelayCommand(() =>
            {
                IsOpen = false;
                _ = Launcher.LaunchUriAsync(link);
            });

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationLong));
        });
    }

    /// <summary>
    /// Handles a notification that saving a video frame snapshot failed.
    /// </summary>
    public void Receive(FailedToSaveFrameNotificationMessage message)
    {
        ShowErrorNotification(NotificationKind.FrameSaveFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that loading a subtitle file failed.
    /// </summary>
    public void Receive(FailedToLoadSubtitleNotificationMessage message)
    {
        ShowErrorNotification(NotificationKind.SubtitleLoadFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that opening files or a folder for playback failed.
    /// </summary>
    public void Receive(FailedToOpenFilesNotificationMessage message)
    {
        ShowErrorNotification(NotificationKind.FileOpenFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that adding a folder to a media library failed.
    /// </summary>
    public void Receive(FailedToAddFolderNotificationMessage message)
    {
        ShowErrorNotification(NotificationKind.FolderAddFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that media player initialization failed.
    /// </summary>
    public void Receive(FailedToInitializeNotificationMessage message)
    {
        ShowErrorNotification(NotificationKind.InitializationFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that a playlist was created.
    /// </summary>
    public void Receive(PlaylistCreatedNotificationMessage message)
    {
        ShowSuccessNotification(NotificationKind.PlaylistCreated, title: message.PlaylistName);
    }

    /// <summary>
    /// Handles a notification that a playlist was deleted.
    /// </summary>
    public void Receive(PlaylistDeletedNotificationMessage message)
    {
        ShowSuccessNotification(NotificationKind.PlaylistDeleted, title: message.PlaylistName);
    }

    /// <summary>
    /// Handles a notification that a playlist was renamed.
    /// </summary>
    public void Receive(PlaylistRenamedNotificationMessage message)
    {
        ShowSuccessNotification(NotificationKind.PlaylistRenamed, title: message.NewName);
    }

    /// <summary>
    /// Handles a notification that items were added to a playlist.
    /// </summary>
    public void Receive(PlaylistItemsAddedNotificationMessage message)
    {
        ShowSuccessNotification(NotificationKind.PlaylistItemsAdded, title: message.PlaylistName, count: message.ItemCount);
    }

    private void ShowSuccessNotification(NotificationKind kind, string? title = null, string? message = null, int count = 0)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Kind = kind;
            Severity = NotificationLevel.Success;
            Title = title;
            Message = message;
            Count = count;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationShort));
        });
    }

    private void ShowErrorNotification(NotificationKind kind, string? message)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Kind = kind;
            Severity = NotificationLevel.Error;
            Message = message;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationLong));
        });
    }

    private void ShowErrorNotification(string? title, string? message)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Kind = NotificationKind.None;
            Severity = NotificationLevel.Error;
            Title = title;
            Message = message;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(NotificationDurationLong));
        });
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
        Count = default;
        Message = default;
        ActionContent = default;
        ActionCommand = default;
        IsOpen = false;
    }
}
