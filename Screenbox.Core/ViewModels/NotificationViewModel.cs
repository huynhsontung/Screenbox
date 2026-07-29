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

        Messenger.Register<RaiseFrameSavedNotificationMessage>(this);
        Messenger.Register<RaiseResumePositionNotificationMessage>(this);
        Messenger.Register<RaiseLibraryAccessDeniedNotificationMessage>(this);
        Messenger.Register<MediaLoadFailedNotificationMessage>(this);
        Messenger.Register<CloseNotificationMessage>(this);
        Messenger.Register<SubtitleAddedNotificationMessage>(this);
        Messenger.Register<ErrorMessage>(this);
        Messenger.Register<FailedToSaveFrameNotificationMessage>(this);
        Messenger.Register<FailedToLoadSubtitleNotificationMessage>(this);
        Messenger.Register<FailedToOpenFilesNotificationMessage>(this);
        Messenger.Register<FailedToAddFolderNotificationMessage>(this);
        Messenger.Register<FailedToInitializeNotificationMessage>(this);
        Messenger.Register<PlaylistCreatedNotificationMessage>(this);
        Messenger.Register<PlaylistDeletedNotificationMessage>(this);
        Messenger.Register<PlaylistRenamedNotificationMessage>(this);
        Messenger.Register<PlaylistItemsAddedNotificationMessage>(this);
    }

    /// <summary>
    /// Handles a general error message.
    /// </summary>
    public void Receive(ErrorMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.None, title: message.Title, message: message.Message);
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
        ShowNotification(NotificationLevel.Success, NotificationKind.SubtitleAdded, message: message.File.Name);
    }

    /// <summary>
    /// Handles a notification indicating media failed to load.
    /// </summary>
    public void Receive(MediaLoadFailedNotificationMessage message)
    {
        string body = string.IsNullOrEmpty(message.Reason) || string.IsNullOrEmpty(message.Path)
            ? $"{message.Path}{message.Reason}"
            : $"{message.Path}{Environment.NewLine}{message.Reason}";
        ShowNotification(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: body);
    }

    /// <summary>
    /// Handles a notification indicating a frame was saved.
    /// </summary>
    public void Receive(RaiseFrameSavedNotificationMessage message)
    {
        ShowNotification(
            NotificationLevel.Success,
            NotificationKind.FrameSaved,
            actionContent: message.Value.Name,
            actionCommand: new RelayCommand(() => _filesService.OpenFileLocationAsync(message.Value)));
    }

    /// <summary>
    /// Handles a notification to resume media at a previous position.
    /// </summary>
    public void Receive(RaiseResumePositionNotificationMessage message)
    {
        if (Severity is NotificationLevel.Error && IsOpen)
            return;

        ShowNotification(
            NotificationLevel.Info,
            NotificationKind.ResumePosition,
            actionContent: Humanizer.ToDuration(message.Value),
            actionCommand: new RelayCommand(() =>
            {
                IsOpen = false;
                Messenger.Send(new ChangeTimeRequestMessage(message.Value, debounce: false));
            }));
    }

    /// <summary>
    /// Handles a notification indicating library access was denied.
    /// </summary>
    public void Receive(RaiseLibraryAccessDeniedNotificationMessage message)
    {
        if (message.Library is not (KnownLibraryId.Music or KnownLibraryId.Pictures or KnownLibraryId.Videos))
            return;

        NotificationKind kind = message.Library switch
        {
            KnownLibraryId.Music => NotificationKind.MusicLibraryAccessDenied,
            KnownLibraryId.Pictures => NotificationKind.PicturesLibraryAccessDenied,
            KnownLibraryId.Videos => NotificationKind.VideosLibraryAccessDenied,
            _ => NotificationKind.None,
        };

        ShowNotification(NotificationLevel.Error, kind);
    }

    /// <summary>
    /// Handles a notification that saving a video frame snapshot failed.
    /// </summary>
    public void Receive(FailedToSaveFrameNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.FrameSaveFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that loading a subtitle file failed.
    /// </summary>
    public void Receive(FailedToLoadSubtitleNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.SubtitleLoadFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that opening files or a folder for playback failed.
    /// </summary>
    public void Receive(FailedToOpenFilesNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.FileOpenFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that adding a folder to a media library failed.
    /// </summary>
    public void Receive(FailedToAddFolderNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.FolderAddFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that media player initialization failed.
    /// </summary>
    public void Receive(FailedToInitializeNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Error, NotificationKind.InitializationFailed, message: message.Reason);
    }

    /// <summary>
    /// Handles a notification that a playlist was created.
    /// </summary>
    public void Receive(PlaylistCreatedNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Success, NotificationKind.PlaylistCreated, title: message.PlaylistName);
    }

    /// <summary>
    /// Handles a notification that a playlist was deleted.
    /// </summary>
    public void Receive(PlaylistDeletedNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Success, NotificationKind.PlaylistDeleted, title: message.PlaylistName);
    }

    /// <summary>
    /// Handles a notification that a playlist was renamed.
    /// </summary>
    public void Receive(PlaylistRenamedNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Success, NotificationKind.PlaylistRenamed, title: message.NewName);
    }

    /// <summary>
    /// Handles a notification that items were added to a playlist.
    /// </summary>
    public void Receive(PlaylistItemsAddedNotificationMessage message)
    {
        ShowNotification(NotificationLevel.Success, NotificationKind.PlaylistItemsAdded, title: message.PlaylistName, count: message.ItemCount);
    }

    private void ShowNotification(
        NotificationLevel level,
        NotificationKind kind,
        string? title = null,
        string? message = null,
        int count = 0,
        string? actionContent = null,
        ICommand? actionCommand = null)
    {
        var duration = GetNotificationDuration(level);

        _dispatcherQueue.TryEnqueue(() =>
        {
            Reset();
            Severity = level;
            Kind = kind;
            Title = title;
            Message = message;
            Count = count;
            ActionContent = actionContent;
            ActionCommand = actionCommand;

            IsOpen = true;
            _timer.Debounce(() => IsOpen = false, duration);
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
