using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class NotificationView : UserControl
{
    private NotificationViewModel ViewModel => (NotificationViewModel)DataContext;

    public NotificationView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<NotificationViewModel>();
    }

    private ButtonBase? GetActionButton(NotificationKind kind, string? actionContent, ICommand? actionCommand)
    {
        return kind switch
        {
            NotificationKind.MusicLibraryAccessDenied or
            NotificationKind.PicturesLibraryAccessDenied or
            NotificationKind.VideosLibraryAccessDenied => new HyperlinkButton
            {
                Content = Strings.Resources.OpenPrivacySettingsButtonText,
                Command = actionCommand,
            },
            NotificationKind.FrameSaved => new HyperlinkButton
            {
                Content = actionContent,
                Command = actionCommand,
            },
            NotificationKind.ResumePosition => new Button
            {
                Content = Strings.Resources.GoToPosition(actionContent),
                Command = actionCommand,
            },
            _ => null
        };
    }

    private string? GetDisplayTitle(NotificationKind kind, string? title, int count)
    {
        return kind switch
        {
            NotificationKind.None => title,
            NotificationKind.MusicLibraryAccessDenied => Strings.Resources.AccessDeniedMusicLibraryTitle,
            NotificationKind.PicturesLibraryAccessDenied => Strings.Resources.AccessDeniedPicturesLibraryTitle,
            NotificationKind.VideosLibraryAccessDenied => Strings.Resources.AccessDeniedVideosLibraryTitle,
            NotificationKind.InitializationFailed => Strings.Resources.FailedToInitializeNotificationTitle,
            NotificationKind.FileOpenFailed => Strings.Resources.FailedToOpenFilesNotificationTitle,
            NotificationKind.FolderAddFailed => Strings.Resources.FailedToAddFolderNotificationTitle,
            NotificationKind.MediaLoadFailed => Strings.Resources.FailedToLoadMediaNotificationTitle,
            NotificationKind.SubtitleLoadFailed => Strings.Resources.FailedToLoadSubtitleNotificationTitle,
            NotificationKind.FrameSaveFailed => Strings.Resources.FailedToSaveFrameNotificationTitle,
            NotificationKind.FrameSaved => Strings.Resources.FrameSavedNotificationTitle,
            NotificationKind.SubtitleAdded => Strings.Resources.SubtitleAddedNotificationTitle,
            NotificationKind.PlaylistCreated => Strings.Resources.PlaylistCreatedNotificationTitle(title ?? string.Empty),
            NotificationKind.PlaylistDeleted => Strings.Resources.PlaylistDeletedNotificationTitle(title ?? string.Empty),
            NotificationKind.PlaylistRenamed => Strings.Resources.PlaylistRenamedNotificationTitle(title ?? string.Empty),
            NotificationKind.PlaylistItemsAdded => Strings.Resources.PlaylistItemsAddedNotificationTitle(count, title ?? string.Empty),
            NotificationKind.ResumePosition => Strings.Resources.ResumePositionNotificationTitle,
            _ => null,
        };
    }

    private string? GetDisplayMessage(NotificationKind kind, string? message)
    {
        return kind switch
        {
            NotificationKind.None => message,
            NotificationKind.MusicLibraryAccessDenied => Strings.Resources.AccessDeniedMessage,
            NotificationKind.PicturesLibraryAccessDenied => Strings.Resources.AccessDeniedMessage,
            NotificationKind.VideosLibraryAccessDenied => Strings.Resources.AccessDeniedMessage,
            NotificationKind.InitializationFailed => message,
            NotificationKind.FileOpenFailed => message,
            NotificationKind.FolderAddFailed => message,
            NotificationKind.MediaLoadFailed => message,
            NotificationKind.SubtitleLoadFailed => message,
            NotificationKind.FrameSaveFailed => message,
            NotificationKind.SubtitleAdded => message,
            _ => null,
        };
    }

    private InfoBarSeverity ConvertInfoBarSeverity(NotificationLevel level)
    {
        return level switch
        {
            NotificationLevel.Error => InfoBarSeverity.Error,
            NotificationLevel.Warning => InfoBarSeverity.Warning,
            NotificationLevel.Success => InfoBarSeverity.Success,
            _ => InfoBarSeverity.Informational
        };
    }
}
