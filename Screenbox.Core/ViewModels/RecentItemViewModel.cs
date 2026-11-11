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
    /// Gets or sets the display name of the recent item.
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>
    /// Gets or sets the secondary subtitle (e.g., album artist).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArtistName))]
    public partial string? Subtitle { get; set; }

    /// <summary>
    /// Gets the artist name for album cards.
    /// </summary>
    public string ArtistName => LinkedAlbum?.ArtistName ?? Subtitle ?? string.Empty;

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
    /// Gets the album art for album cards.
    /// </summary>
    public BitmapImage? AlbumArt => LinkedAlbum?.AlbumArt;

    /// <summary>
    /// Gets the media type for single media items.
    /// </summary>
    public MediaPlaybackType MediaType => Media?.MediaType ?? MediaPlaybackType.Unknown;

    /// <summary>
    /// Gets the caption for single media items.
    /// </summary>
    public string Caption => Media?.Caption ?? string.Empty;

    /// <summary>
    /// Gets the item count for playlists (bound as <see cref="double"/> for localization pluralization).
    /// </summary>
    public double ItemsCount => Kind switch
    {
        RecentItemKind.Playlist when LinkedPlaylist is not null => LinkedPlaylist.ItemsCount,
        _ => _snapshotItems.Count
    };

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
    /// Initializes a new instance of the <see cref="RecentItemViewModel"/> class for a single media item.
    /// </summary>
    public RecentItemViewModel(string entryId, string contentHash, MediaViewModel media, Guid? activeSessionId = null)
    {
        EntryId = entryId;
        ContentHash = contentHash;
        Kind = RecentItemKind.SingleMedia;
        Media = media;
        SourceId = media.Location;
        Name = media.Name;
        ActiveSessionId = activeSessionId;
        _snapshotItems = [media];

        media.PropertyChanged += Media_PropertyChanged;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentItemViewModel"/> class for a container or playlist entry.
    /// </summary>
    public RecentItemViewModel(
        string entryId,
        string contentHash,
        RecentItemKind kind,
        string name,
        string? subtitle,
        string? sourceId,
        IReadOnlyList<MediaViewModel> snapshotItems,
        int initialIndex = 0,
        Guid? activeSessionId = null)
    {
        EntryId = entryId;
        ContentHash = contentHash;
        Kind = kind;
        Name = name;
        Subtitle = subtitle;
        SourceId = sourceId;
        InitialIndex = initialIndex;
        ActiveSessionId = activeSessionId;
        _snapshotItems = snapshotItems.ToList();
    }

    /// <summary>
    /// Updates the snapshot items and metadata for an ad-hoc playlist.
    /// </summary>
    public void UpdateMediaCollectionSnapshot(string name, string? sourceId, IReadOnlyList<MediaViewModel> items, int initialIndex)
    {
        Name = name;
        SourceId = sourceId;
        InitialIndex = initialIndex;
        _snapshotItems = items.ToList();
        OnPropertyChanged(nameof(ItemsCount));
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
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
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
        OnPropertyChanged(nameof(ArtistName));
        OnPropertyChanged(nameof(AlbumArt));
        OnPropertyChanged(nameof(Thumbnail));
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
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
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
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

        switch (Kind)
        {
            case RecentItemKind.SingleMedia when Media is not null:
                Messenger.Send(new PlayMediaMessage(Media, false));
                break;

            case RecentItemKind.MediaCollection:
                if (_snapshotItems.Count > 0)
                {
                    int index = Math.Clamp(InitialIndex, 0, _snapshotItems.Count - 1);
                    var origin = QueueOrigin.ForMediaCollection(Name, SourceId);
                    var queue = new QueueSession(index, _snapshotItems) { Origin = origin };
                    ActiveSessionId = queue.SessionId;
                    Messenger.Send(new SetQueueMessage(queue, true));
                }
                break;

            case RecentItemKind.Playlist:
                {
                    var items = GetPlayableItems();
                    if (items.Count > 0)
                    {
                        var origin = LinkedPlaylist is not null
                            ? QueueOrigin.ForPlaylist(LinkedPlaylist)
                            : QueueOrigin.ForMediaCollection(Name, SourceId);
                        var queue = new QueueSession(0, items) { Origin = origin };
                        ActiveSessionId = queue.SessionId;
                        Messenger.Send(new SetQueueMessage(queue, true));
                    }
                }
                break;

            case RecentItemKind.Album:
                {
                    var items = GetPlayableItems();
                    if (items.Count > 0)
                    {
                        var origin = LinkedAlbum is not null
                            ? QueueOrigin.ForAlbum(LinkedAlbum)
                            : QueueOrigin.ForMediaCollection(Name, SourceId);
                        var queue = new QueueSession(0, items) { Origin = origin };
                        ActiveSessionId = queue.SessionId;
                        Messenger.Send(new SetQueueMessage(queue, true));
                    }
                }
                break;

            case RecentItemKind.Artist:
                {
                    var items = GetPlayableItems();
                    if (items.Count > 0)
                    {
                        var origin = LinkedArtist is not null
                            ? QueueOrigin.ForArtist(LinkedArtist)
                            : QueueOrigin.ForMediaCollection(Name, SourceId);
                        var queue = new QueueSession(0, items) { Origin = origin };
                        ActiveSessionId = queue.SessionId;
                        Messenger.Send(new SetQueueMessage(queue, true));
                    }
                }
                break;
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

    private void Media_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MediaViewModel.Name):
                if (Media is not null) Name = Media.Name;
                break;
            case nameof(MediaViewModel.Thumbnail):
                OnPropertyChanged(nameof(Thumbnail));
                break;
            case nameof(MediaViewModel.MediaType):
                OnPropertyChanged(nameof(MediaType));
                break;
            case nameof(MediaViewModel.Caption):
                OnPropertyChanged(nameof(Caption));
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
        }
        else if (e.PropertyName == nameof(PlaylistViewModel.ItemsCount))
        {
            OnPropertyChanged(nameof(ItemsCount));
        }
    }

    private void LinkedPlaylistItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
    }

    private void LinkedAlbum_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LinkedAlbum is null) return;
        if (e.PropertyName == nameof(AlbumViewModel.AlbumArt))
        {
            OnPropertyChanged(nameof(AlbumArt));
            OnPropertyChanged(nameof(Thumbnail));
        }
        else if (e.PropertyName == nameof(AlbumViewModel.ArtistName))
        {
            Subtitle = LinkedAlbum.ArtistName;
            OnPropertyChanged(nameof(ArtistName));
        }
    }

    private void LinkedArtist_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (LinkedArtist is null) return;
        if (e.PropertyName == nameof(ArtistViewModel.Name))
        {
            Name = LinkedArtist.Name;
        }
    }

    private void LinkedEntitySongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LinkedAlbum?.UpdateProperties();
        OnPropertyChanged(nameof(TargetArtist));
        PlayCommand.NotifyCanExecuteChanged();
        PlayNextCommand.NotifyCanExecuteChanged();
        AddToQueueCommand.NotifyCanExecuteChanged();
    }
}
