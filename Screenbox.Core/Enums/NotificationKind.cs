namespace Screenbox.Core.Enums;

public enum NotificationKind
{
    None,
    Generic,

    AccessDeniedMusicLibrary,
    AccessDeniedPicturesLibrary,
    AccessDeniedVideosLibrary,

    FailedToAddFolder,
    FailedToInitialize,
    FailedToLoadMedia,
    FailedToLoadSubtitle,
    FailedToSaveFrame,
    FailedToOpenFiles,
    
    FrameSaved,

    PlaylistCreated,
    PlaylistDeleted,
    PlaylistRenamed,
    PlaylistItemsAdded,

    ResumePosition,

    SubtitleAdded,
}
