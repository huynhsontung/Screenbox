using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class AlbumViewModel : ObservableRecipient,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    public string Name { get; }

    public string ArtistName => string.IsNullOrEmpty(_albumArtist)
        ? _songArtist
        : _albumArtist;

    public string AlbumKey => AlbumViewModelFactory.GetAlbumKey(Name, ArtistName);

    public uint? Year
    {
        get => _year;
        set
        {
            if (value > 0)
            {
                _year = value;
            }
        }
    }

    public BitmapImage? AlbumArt => _albumArt;

    public DateTimeOffset DateAdded { get; set; }

    public ObservableCollection<MediaViewModel> RelatedSongs { get; }

    public IReadOnlyList<MediaViewModel> OrderedSongs => GetSortedSongs(RelatedSongs);

    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }

    private readonly string _albumArtist;
    private string _songArtist;
    private BitmapImage? _albumArt;
    private uint? _year;

    public AlbumViewModel() : this(string.Empty, string.Empty, WeakReferenceMessenger.Default) { }

    public AlbumViewModel(string album, string albumArtist) : this(album, albumArtist, WeakReferenceMessenger.Default) { }

    public AlbumViewModel(string album, string albumArtist, IMessenger messenger) : base(messenger)
    {
        Name = album;
        _albumArtist = albumArtist;
        _songArtist = string.Empty;
        RelatedSongs = new ObservableCollection<MediaViewModel>();
        RelatedSongs.CollectionChanged += RelatedSongsOnCollectionChanged;

        Messenger.Register<QueuePlaybackStateChangedMessage>(this);

        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
    }

    public void Receive(QueuePlaybackStateChangedMessage message)
    {
        UpdatePlaybackState(message.State);
    }

    private void UpdatePlaybackState(QueuePlaybackState state)
    {
        bool isActive = state.SessionId != Guid.Empty &&
                        state.CurrentItem is not null &&
                        (state.Origin?.Matches(RecentItemKind.Album, AlbumKey) ?? false);
        IsQueueActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    public async Task LoadAlbumArtAsync()
    {
        if (RelatedSongs.Count > 0)
        {
            await RelatedSongs[0].LoadThumbnailAsync();
            UpdateProperties();
        }
    }

    private void RelatedSongsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateProperties();
    }

    private void UpdateProperties()
    {
        if (RelatedSongs.Count == 0) return;
        MediaViewModel first = RelatedSongs[0];
        SetProperty(ref _albumArt, first.Thumbnail, nameof(AlbumArt));
        SetProperty(ref _songArtist, first.MainArtist?.Name ?? string.Empty, nameof(ArtistName));
    }

    [RelayCommand]
    private void PlayAlbum()
    {
        if (RelatedSongs.Count == 0)
            return;

        if (IsQueueActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
        }
        else
        {
            IReadOnlyList<MediaViewModel> songs = OrderedSongs;
            Messenger.SendQueueAndPlay(songs[0], songs, pauseIfExists: false, origin: PlaylistOrigin.ForAlbum(this));
        }
    }

    [RelayCommand]
    private void PlayAlbumNext()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendPlayNext(OrderedSongs);
    }

    [RelayCommand]
    private void AddAlbumToQueue()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendAddToQueue(OrderedSongs);
    }

    private static IReadOnlyList<MediaViewModel> GetSortedSongs(IEnumerable<MediaViewModel> songs)
    {
        return songs
            .OrderBy(m => m.MediaInfo.MusicProperties.TrackNumber)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}
