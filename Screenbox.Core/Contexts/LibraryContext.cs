#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Windows.Storage;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the state for library management operations
/// </summary>
public sealed partial class LibraryContext : ObservableRecipient
{
    [ObservableProperty]
    private StorageLibrary? _musicStorageLibrary;

    [ObservableProperty]
    private StorageLibrary? _videosStorageLibrary;

    [ObservableProperty]
    private bool _isLoadingVideos;

    [ObservableProperty]
    private bool _isLoadingMusic;

    [ObservableProperty]
    private MusicLibrary _music = MusicLibrary.Empty;

    [ObservableProperty]
    private VideosLibrary _videos = VideosLibrary.Empty;


    public void RaiseMusicLibraryContentChanged()
    {
        Messenger.Send(new LibraryContentChangedMessage(KnownLibraryId.Music));
    }

    public void RaiseVideosLibraryContentChanged()
    {
        Messenger.Send(new LibraryContentChangedMessage(KnownLibraryId.Videos));
    }
}

