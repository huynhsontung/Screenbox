#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class NotificationView : UserControl
    {
        private NotificationViewModel ViewModel => (NotificationViewModel)DataContext;

        public NotificationView()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<NotificationViewModel>();
        }

        private ButtonBase? GetActionButton(NotificationKind kind, string? actionContent, RelayCommand? actionCommand)
        {
            if (actionCommand is null)
            {
                return null;
            }

            return kind switch
            {
                NotificationKind.AccessDeniedMusicLibrary or
                NotificationKind.AccessDeniedPicturesLibrary or
                NotificationKind.AccessDeniedVideosLibrary => new HyperlinkButton
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
                NotificationKind.None => null,
                NotificationKind.Generic => title,
                NotificationKind.AccessDeniedMusicLibrary => Strings.Resources.AccessDeniedMusicLibraryTitle,
                NotificationKind.AccessDeniedPicturesLibrary => Strings.Resources.AccessDeniedPicturesLibraryTitle,
                NotificationKind.AccessDeniedVideosLibrary => Strings.Resources.AccessDeniedVideosLibraryTitle,
                NotificationKind.FailedToAddFolder => Strings.Resources.FailedToAddFolderNotificationTitle,
                NotificationKind.FailedToInitialize => Strings.Resources.FailedToInitializeNotificationTitle,
                NotificationKind.FailedToLoadMedia => Strings.Resources.FailedToLoadMediaNotificationTitle,
                NotificationKind.FailedToLoadSubtitle => Strings.Resources.FailedToLoadSubtitleNotificationTitle,
                NotificationKind.FailedToOpenFiles => Strings.Resources.FailedToOpenFilesNotificationTitle,
                NotificationKind.FailedToSaveFrame => Strings.Resources.FailedToSaveFrameNotificationTitle,
                NotificationKind.FrameSaved => Strings.Resources.FrameSavedNotificationTitle,
                NotificationKind.PlaylistCreated => Strings.Resources.PlaylistCreatedNotificationTitle(title ?? string.Empty),
                NotificationKind.PlaylistDeleted => Strings.Resources.PlaylistDeletedNotificationTitle(title ?? string.Empty),
                NotificationKind.PlaylistRenamed => Strings.Resources.PlaylistRenamedNotificationTitle(title ?? string.Empty),
                NotificationKind.PlaylistItemsAdded => Strings.Resources.PlaylistItemsAddedNotificationTitle(count, title ?? string.Empty),
                NotificationKind.ResumePosition => Strings.Resources.ResumePositionNotificationTitle,
                NotificationKind.SubtitleAdded => Strings.Resources.SubtitleAddedNotificationTitle,
                _ => null,
            };
        }

        private string? GetDisplayMessage(NotificationKind kind, string? message)
        {
            return kind switch
            {
                NotificationKind.None => null,
                NotificationKind.Generic => message,
                NotificationKind.AccessDeniedMusicLibrary => Strings.Resources.AccessDeniedMessage,
                NotificationKind.AccessDeniedPicturesLibrary => Strings.Resources.AccessDeniedMessage,
                NotificationKind.AccessDeniedVideosLibrary => Strings.Resources.AccessDeniedMessage,
                NotificationKind.FailedToAddFolder => message,
                NotificationKind.FailedToInitialize => message,
                NotificationKind.FailedToLoadMedia => message,
                NotificationKind.FailedToLoadSubtitle => message,
                NotificationKind.FailedToOpenFiles => message,
                NotificationKind.FailedToSaveFrame => message,
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
}
