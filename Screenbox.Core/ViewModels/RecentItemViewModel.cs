using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents an entry in the Recent Media list on the Home page.
/// Supports single media items, ad-hoc playlists, user playlists, albums, and artists.
/// </summary>
public sealed partial class RecentItemViewModel : ObservableRecipient
{
    /// <summary>
    /// Gets the persistent SQLite entry identifier.
    /// </summary>
    public string EntryId { get; }

    /// <summary>
    /// Gets the deterministic content hash used for deduplication.
    /// </summary>
    public string ContentHash { get; }

    /// <summary>
    /// Gets the kind of recent item.
    /// </summary>
    public RecentItemKind Kind { get; }

    /// <summary>
    /// Gets or sets the session ID of the play queue session currently associated with this recent item, if any.
    /// </summary>
    public Guid? ActiveSessionId { get; set; }

    /// <summary>
    /// Gets the underlying <see cref="MediaViewModel"/> when <see cref="Kind"/> is <see cref="RecentItemKind.SingleMedia"/>.
    /// </summary>
    public MediaViewModel? Media { get; }

    /// <summary>
    /// Gets the source path or identifier (e.g., folder path, playlist ID, album key, artist key).
    /// </summary>
    public string? SourceId { get; private set; }

    /// <summary>
    /// Gets the queue origin for this recent item, reflecting any live-linked playlist, album, or artist.
    /// </summary>
    public QueueOrigin Origin => Kind switch
    {
        RecentItemKind.SingleMedia when Media is not null => QueueOrigin.ForSingleMedia(Media),
        RecentItemKind.Playlist when LinkedPlaylist is not null => QueueOrigin.ForPlaylist(LinkedPlaylist),
        RecentItemKind.Album when LinkedAlbum is not null => QueueOrigin.ForAlbum(LinkedAlbum),
        RecentItemKind.Artist when LinkedArtist is not null => QueueOrigin.ForArtist(LinkedArtist),
        _ => new QueueOrigin(Kind, SourceId ?? string.Empty, Name, Subtitle)
    };

    /// <summary>
    /// Gets or sets the display name of the recent item.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Origin))]
    public partial string Name { get; set; }

    /// <summary>
    /// Gets or sets the secondary subtitle (e.g., album artist).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Caption))]
    [NotifyPropertyChangedFor(nameof(Origin))]
    public partial string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this recent item's queue session or media is currently active in the play queue.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayingOrActive))]
    public partial bool IsQueueActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the active item in this recent item's session is currently playing.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlayingOrActive))]
    public partial bool IsPlaying { get; set; }

    /// <summary>
    /// Gets a value indicating whether this recent item is currently playing or active.
    /// </summary>
    public bool IsPlayingOrActive => IsPlaying || IsQueueActive;

    /// <summary>
    /// Gets the thumbnail source for the card.
    /// Returns <see langword="null"/> for playlists so they always use the playlist glyph.
    /// </summary>
    public BitmapImage? Thumbnail => Kind switch
    {
        RecentItemKind.SingleMedia => Media?.Thumbnail,
        RecentItemKind.Album => LinkedAlbum?.AlbumArt,
        _ => null
    };

    /// <summary>
    /// Gets the media type for single media items.
    /// </summary>
    public MediaPlaybackType MediaType => Media?.MediaType ?? MediaPlaybackType.Unknown;

    /// <summary>
    /// Gets the caption for this recent item.
    /// Returns media caption for single media, artist name for albums, or subtitle.
    /// </summary>
    public string Caption => Kind switch
    {
        RecentItemKind.SingleMedia => Media?.Caption ?? string.Empty,
        RecentItemKind.Album => LinkedAlbum?.ArtistName ?? Subtitle ?? string.Empty,
        _ => Subtitle ?? string.Empty
    };

    /// <summary>
    /// Gets the item count for playlists (bound as <see cref="double"/> for localization pluralization).
    /// </summary>
    public double ItemsCount => Kind switch
    {
        RecentItemKind.Playlist when LinkedPlaylist is not null => LinkedPlaylist.ItemsCount,
        _ => _snapshotItems.Count
    };

    /// <summary>
    /// Gets the corner radius for the thumbnail presentation.
    /// Uses circular radius for artists and standard rounded rectangle for other media.
    /// </summary>
    public CornerRadius CornerRadius => Kind == RecentItemKind.Artist
        ? new CornerRadius(99)
        : new CornerRadius(4);

    /// <summary>
    /// Gets the horizontal text alignment for the title and caption.
    /// Centered for artists and left-aligned for other media.
    /// </summary>
    public TextAlignment HorizontalTextAlignment => Kind == RecentItemKind.Artist
        ? TextAlignment.Center
        : TextAlignment.Left;

    /// <summary>
    /// Gets the initial index for re-playing an ad-hoc playlist.
    /// </summary>
    public int InitialIndex { get; private set; }

    /// <summary>
    /// Gets the linked <see cref="PlaylistViewModel"/> when <see cref="Kind"/> is <see cref="RecentItemKind.Playlist"/>.
    /// </summary>
    public PlaylistViewModel? LinkedPlaylist { get; private set; }

    /// <summary>
    /// Gets the linked <see cref="AlbumViewModel"/> when <see cref="Kind"/> is <see cref="RecentItemKind.Album"/>.
    /// </summary>
    public AlbumViewModel? LinkedAlbum { get; private set; }

    /// <summary>
    /// Gets the linked <see cref="ArtistViewModel"/> when <see cref="Kind"/> is <see cref="RecentItemKind.Artist"/>.
    /// </summary>
    public ArtistViewModel? LinkedArtist { get; private set; }

    /// <summary>
    /// Gets the linked album for context menu navigation (either <see cref="LinkedAlbum"/> or the single media's album).
    /// </summary>
    public AlbumViewModel? TargetAlbum => Kind switch
    {
        RecentItemKind.Album => LinkedAlbum,
        RecentItemKind.SingleMedia => Media?.Album,
        _ => null
    };

    /// <summary>
    /// Gets the linked artist for context menu navigation (either <see cref="LinkedArtist"/>, the linked album's main artist, or the single media's main artist).
    /// </summary>
    public ArtistViewModel? TargetArtist => Kind switch
    {
        RecentItemKind.Artist => LinkedArtist,
        RecentItemKind.Album => LinkedAlbum?.RelatedSongs.FirstOrDefault()?.MainArtist,
        RecentItemKind.SingleMedia => Media?.MainArtist,
        _ => null
    };

    private List<MediaViewModel> _snapshotItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentItemViewModel"/> class from a <see cref="QueueOrigin"/>.
    /// </summary>
    public RecentItemViewModel(
        string entryId,
        string contentHash,
        QueueOrigin origin,
        IReadOnlyList<MediaViewModel>? snapshotItems = null,
        int initialIndex = 0,
        Guid? activeSessionId = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        EntryId = entryId;
        ContentHash = contentHash;
        Kind = origin.Kind;
        Name = origin.Title;
        Subtitle = origin.Subtitle;
        SourceId = origin.SourceId;
        InitialIndex = initialIndex;
        ActiveSessionId = activeSessionId;
        _snapshotItems = snapshotItems is not null ? [..snapshotItems] : [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentItemViewModel"/> class for a single media item.
    /// </summary>
    public RecentItemViewModel(string entryId, string contentHash, MediaViewModel media, Guid? activeSessionId = null)
        : this(entryId, contentHash, QueueOrigin.ForSingleMedia(media), (MediaViewModel[])[media], 0, activeSessionId)
    {
        Media = media;
        media.PropertyChanged += Media_PropertyChanged;
    }

    /// <summary>
    /// Updates the snapshot items and metadata for an ad-hoc playlist.
    /// </summary>
    public void UpdateMediaCollectionSnapshot(QueueOrigin origin, IReadOnlyList<MediaViewModel> items, int initialIndex)
    {
        ArgumentNullException.ThrowIfNull(origin);
        Name = origin.Title;
        SourceId = origin.SourceId;
        InitialIndex = initialIndex;
        _snapshotItems = [..items];
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(Origin));
    }

    /// <summary>
    /// Links this recent entry to a live <see cref="PlaylistViewModel"/>.
    /// </summary>
    public void LinkPlaylist(PlaylistViewModel? playlist)
    {
        if (ReferenceEquals(LinkedPlaylist, playlist)) return;

        if (LinkedPlaylist is not null)
        {
            LinkedPlaylist.PropertyChanged -= LinkedPlaylist_PropertyChanged;
            LinkedPlaylist.Items.CollectionChanged -= LinkedPlaylistItems_CollectionChanged;
        }

        LinkedPlaylist = playlist;

        if (playlist is not null)
        {
            Name = playlist.Name;
            playlist.PropertyChanged += LinkedPlaylist_PropertyChanged;
            playlist.Items.CollectionChanged += LinkedPlaylistItems_CollectionChanged;
        }

        OnPropertyChanged(nameof(LinkedPlaylist));
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(Origin));
        NotifyPlaybackCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Links this recent entry to a live <see cref="AlbumViewModel"/>.
    /// </summary>
    public void LinkAlbum(AlbumViewModel? album)
    {
        if (ReferenceEquals(LinkedAlbum, album)) return;

        if (LinkedAlbum is not null)
        {
            LinkedAlbum.PropertyChanged -= LinkedAlbum_PropertyChanged;
            LinkedAlbum.RelatedSongs.CollectionChanged -= LinkedEntitySongs_CollectionChanged;
        }

        LinkedAlbum = album;

        if (album is not null)
        {
            album.UpdateProperties();
            Name = album.Name;
            Subtitle = album.ArtistName;
            album.PropertyChanged += LinkedAlbum_PropertyChanged;
            album.RelatedSongs.CollectionChanged += LinkedEntitySongs_CollectionChanged;
        }

        OnPropertyChanged(nameof(LinkedAlbum));
        OnPropertyChanged(nameof(TargetAlbum));
        OnPropertyChanged(nameof(TargetArtist));
        OnPropertyChanged(nameof(Caption));
        OnPropertyChanged(nameof(Thumbnail));
        OnPropertyChanged(nameof(Origin));
        NotifyPlaybackCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Links this recent entry to a live <see cref="ArtistViewModel"/>.
    /// </summary>
    public void LinkArtist(ArtistViewModel? artist)
    {
        if (ReferenceEquals(LinkedArtist, artist)) return;

        if (LinkedArtist is not null)
        {
            LinkedArtist.PropertyChanged -= LinkedArtist_PropertyChanged;
            LinkedArtist.RelatedSongs.CollectionChanged -= LinkedEntitySongs_CollectionChanged;
        }

        LinkedArtist = artist;

        if (artist is not null)
        {
            Name = artist.Name;
            artist.PropertyChanged += LinkedArtist_PropertyChanged;
            artist.RelatedSongs.CollectionChanged += LinkedEntitySongs_CollectionChanged;
        }

        OnPropertyChanged(nameof(LinkedArtist));
        OnPropertyChanged(nameof(TargetArtist));
        OnPropertyChanged(nameof(Origin));
        NotifyPlaybackCommandsCanExecuteChanged();
    }

    /// <summary>
    /// Resolves the playable media items for this recent entry.
    /// </summary>
    public IReadOnlyList<MediaViewModel> GetPlayableItems()
    {
        return Kind switch
        {
            RecentItemKind.SingleMedia => Media is not null ? (MediaViewModel[])[Media] : Array.Empty<MediaViewModel>(),
            RecentItemKind.MediaCollection => _snapshotItems,
            RecentItemKind.Playlist => LinkedPlaylist?.Items ?? (IReadOnlyList<MediaViewModel>)_snapshotItems,
            RecentItemKind.Album => LinkedAlbum?.OrderedSongs ?? _snapshotItems,
            RecentItemKind.Artist => LinkedArtist?.OrderedSongs ?? _snapshotItems,
            _ => Array.Empty<MediaViewModel>()
        };
    }

    private bool HasPlayableItems() => GetPlayableItems().Count > 0;

    /// <summary>
    /// Starts playback of this recent item, or toggles play/pause if it is already active in the play queue.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPlayableItems))]
    public void Play()
    {
        if (IsQueueActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
            return;
        }

        if (Kind == RecentItemKind.SingleMedia && Media is not null)
        {
            Messenger.Send(new PlayMediaMessage(Media, false));
            return;
        }

        var items = GetPlayableItems();
        if (items.Count > 0)
        {
            int index = Kind == RecentItemKind.MediaCollection
                ? Math.Clamp(InitialIndex, 0, items.Count - 1)
                : 0;

            var queue = new QueueSession(index, items) { Origin = Origin };
            ActiveSessionId = queue.SessionId;
            Messenger.Send(new SetQueueMessage(queue, true));
        }
    }

    /// <summary>
    /// Inserts this recent item's media next in the play queue.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPlayableItems))]
    public void PlayNext()
    {
        var items = GetPlayableItems();
        if (items.Count > 0)
        {
            Messenger.SendPlayNext(items);
        }
    }

    /// <summary>
    /// Appends this recent item's media to the end of the play queue.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPlayableItems))]
    public void AddToQueue()
    {
        var items = GetPlayableItems();
        if (items.Count > 0)
        {
            Messenger.SendAddToQueue(items);
        }
    }

    /// <summary>
    /// Detaches event handlers from linked models.
    /// </summary>
    public void Detach()
    {
        if (Media is not null)
        {
            Media.PropertyChanged -= Media_PropertyChanged;
        }

        LinkPlaylist(null);
        LinkAlbum(null);
        LinkArtist(null);
    }

    private void NotifyPlaybackCommandsCanExecuteChanged()
    {
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
    }

    private void Media_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MediaViewModel.Name):
                if (Media is not null)
                {
                    Name = Media.Name;
                    OnPropertyChanged(nameof(Origin));
                }
                break;
            case nameof(MediaViewModel.Thumbnail):
                OnPropertyChanged(nameof(Thumbnail));
                break;
            case nameof(MediaViewModel.MediaType):
                OnPropertyChanged(nameof(MediaType));
                break;
            case nameof(MediaViewModel.Caption):
                OnPropertyChanged(nameof(Caption));
                OnPropertyChanged(nameof(Origin));
                break;
            case nameof(MediaViewModel.Album):
                OnPropertyChanged(nameof(TargetAlbum));
                break;
            case nameof(MediaViewModel.MainArtist):
                OnPropertyChanged(nameof(TargetArtist));
                break;
        }
    }

    private void LinkedPlaylist_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LinkedPlaylist is null) return;
        if (e.PropertyName == nameof(PlaylistViewModel.Name))
        {
            Name = LinkedPlaylist.Name;
            OnPropertyChanged(nameof(Origin));
        }
        else if (e.PropertyName == nameof(PlaylistViewModel.ItemsCount))
        {
            OnPropertyChanged(nameof(ItemsCount));
        }
    }

    private void LinkedPlaylistItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
        NotifyPlaybackCommandsCanExecuteChanged();
    }

    private void LinkedAlbum_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LinkedAlbum is null) return;
        if (e.PropertyName == nameof(AlbumViewModel.AlbumArt))
        {
            OnPropertyChanged(nameof(Thumbnail));
        }
        else if (e.PropertyName == nameof(AlbumViewModel.ArtistName))
        {
            Subtitle = LinkedAlbum.ArtistName;
            OnPropertyChanged(nameof(Caption));
            OnPropertyChanged(nameof(Origin));
        }
    }

    private void LinkedArtist_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LinkedArtist is null) return;
        if (e.PropertyName == nameof(ArtistViewModel.Name))
        {
            Name = LinkedArtist.Name;
            OnPropertyChanged(nameof(Origin));
        }
    }

    private void LinkedEntitySongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LinkedAlbum?.UpdateProperties();
        OnPropertyChanged(nameof(TargetArtist));
        NotifyPlaybackCommandsCanExecuteChanged();
    }
}
