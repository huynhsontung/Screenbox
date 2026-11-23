using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.ViewModels;

public sealed partial class ArtistViewModel : ObservableRecipient,
    IRecentItem,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    public ObservableCollection<MediaViewModel> RelatedSongs { get; }

    public string Name { get; }

    public string ArtistKey => ArtistViewModelFactory.GetArtistKey(Name);

    public IReadOnlyList<MediaViewModel> OrderedSongs => GetAlbumSortedSongs(RelatedSongs);

    public bool HasSongs => RelatedSongs.Count > 0;

    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }

    public ArtistViewModel() : this(string.Empty, WeakReferenceMessenger.Default) { }

    public ArtistViewModel(string artist) : this(artist, WeakReferenceMessenger.Default) { }

    public ArtistViewModel(string artist, IMessenger messenger) : base(messenger)
    {
        Name = artist;
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
                        (state.Origin?.Matches(RecentItemKind.Artist, ArtistKey) ?? false);
        IsQueueActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void PlayArtist()
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
            Messenger.SendQueueAndPlay(songs[0], songs, pauseIfExists: false, origin: QueueOrigin.ForArtist(this));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void PlayArtistNext()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendPlayNext(OrderedSongs);
    }

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void AddArtistToQueue()
    {
        if (RelatedSongs.Count == 0)
            return;

        Messenger.SendAddToQueue(OrderedSongs);
    }

    private void RelatedSongsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasSongs));
        PlayArtistCommand.NotifyCanExecuteChanged();
        PlayArtistNextCommand.NotifyCanExecuteChanged();
        AddArtistToQueueCommand.NotifyCanExecuteChanged();
    }

    private static List<MediaViewModel> GetAlbumSortedSongs(IEnumerable<MediaViewModel> songs)
    {
        return songs
            .OrderByDescending(m => m.Album?.Year ?? 0)
            .ThenBy(m => m.MediaInfo.MusicProperties.TrackNumber)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .ToList();
    }
}
