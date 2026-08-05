namespace Screenbox.Core.Enums;

public enum NotificationKind
{
    None,

    MusicLibraryAccessDenied,
    PicturesLibraryAccessDenied,
    VideosLibraryAccessDenied,

    InitializationFailed,
    FileOpenFailed,
    FolderAddFailed,
    MediaLoadFailed,
    SubtitleLoadFailed,
    FrameSaveFailed,

    FrameSaved,
    SubtitleAdded,
    PlaylistCreated,
    PlaylistDeleted,
    PlaylistRenamed,
    PlaylistItemsAdded,

    ResumePosition,
}
