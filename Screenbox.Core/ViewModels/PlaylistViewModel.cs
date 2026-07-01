#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistViewModel : ObservableRecipient
{
    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private object? _thumbnail;
    [ObservableProperty] private DateTimeOffset _lastUpdated = DateTimeOffset.Now;

    public string Id => _id.ToString();

    private Guid _id = Guid.NewGuid();

    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _playlistSaveTimer;

    public PlaylistViewModel(IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _playlistSaveTimer = _dispatcherQueue.CreateTimer();

        Items.CollectionChanged += Items_CollectionChanged;
    }

    private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));

        // Ensures the playlist is up to date when items are reordered, as the ListViewBase
        // reorder operation sends Remove/Add actions instead of a Move action.
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            _playlistSaveTimer.Debounce(() => UpdatePlaylist(), TimeSpan.FromMilliseconds(100));
        }
    }

    public Playlist ToPlaylist()
    {
        return new Playlist(0, Items);
    }

    public void Load(PersistentPlaylistDto persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();
        foreach (var item in persistentPlaylist.Items)
        {
            try
            {
                var vm = ToMediaViewModel(item);
                Items.Add(Items.Contains(vm) ? new MediaViewModel(vm) : vm);
            }
            catch { }
        }
    }

    public async Task SaveAsync()
    {
        LastUpdated = DateTimeOffset.Now;
        var persistentPlaylist = ToPersistentPlaylist();
        await _playlistService.SavePlaylistAsync(persistentPlaylist);
    }

    public async Task RenameAsync(string newDisplayName)
    {
        Name = newDisplayName;
        await SaveAsync();
    }

    [RelayCommand]
    public async Task AddItemsAsync(IReadOnlyList<MediaViewModel> items)
    {
        if (items.Count == 0) return;
        foreach (var item in items)
        {
            Items.Add(Items.Contains(item) ? new MediaViewModel(item) : item);
        }

        //await SaveAsync();
        Messenger.Send(new PlaylistItemsAddedNotificationMessage(Name, items.Count));
    }

    public async Task AddItemsAtIndexAsync(IReadOnlyList<MediaViewModel> items, int insertIndex = -1)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            if (insertIndex < 0 || insertIndex >= Items.Count)
            {
                Items.Add(Items.Contains(item) ? new MediaViewModel(item) : item);
            }
            else
            {
                Items.Insert(insertIndex, Items.Contains(item) ? new MediaViewModel(item) : item);
                insertIndex++;
            }
        }

        //await SaveAsync();
        Messenger.Send(new PlaylistItemsAddedNotificationMessage(Name, items.Count));
    }

    private PersistentPlaylistDto ToPersistentPlaylist()
    {
        return new PersistentPlaylistDto
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(ToRawMediaRecord).ToList()
        };
    }

    private MediaViewModel ToMediaViewModel(RawMediaRecordDto record)
    {
        MediaViewModel media;
        bool existing = false;
        if (Uri.TryCreate(record.Path, UriKind.Absolute, out var uri))
        {
            if (_mediaFactory.TryGetOrCreate(uri, out var existingMedia))
            {
                media = existingMedia!;
                existing = true;
            }
            else
            {
                media = _mediaFactory.GetOrCreate(uri);
            }
        }
        else
        {
            media = _mediaFactory.Create(new Uri("about:blank"));
            media.IsAvailable = false;
        }

        if (!existing)
        {
            if (!string.IsNullOrEmpty(record.Title))
                media.Name = record.Title;

            media.MediaInfo = CreateMediaInfo(record);

            if (record.DateAddedTicks != 0)
            {
                media.DateAdded = new DateTimeOffset(record.DateAddedTicks, TimeSpan.Zero).ToLocalTime();
            }
        }

        return media;
    }

    private static RawMediaRecordDto ToRawMediaRecord(MediaViewModel media)
    {
        return new RawMediaRecordDto
        {
            Path = media.Location,
            Title = media.Name,
            MediaType = media.MediaType,
            DateAddedTicks = media.DateAdded.UtcTicks,
            DurationTicks = media.Duration.Ticks,
            Year = media.MediaType == MediaPlaybackType.Music
                ? media.MediaInfo.MusicProperties.Year
                : media.MediaInfo.VideoProperties.Year,
            Artist = media.MediaInfo.MusicProperties.Artist,
            Album = media.MediaInfo.MusicProperties.Album,
            AlbumArtist = media.MediaInfo.MusicProperties.AlbumArtist,
            Composers = media.MediaInfo.MusicProperties.Composers,
            Genre = media.MediaInfo.MusicProperties.Genre,
            TrackNumber = media.MediaInfo.MusicProperties.TrackNumber,
            Bitrate = media.MediaInfo.MusicProperties.Bitrate,
            Subtitle = media.MediaInfo.VideoProperties.Subtitle,
            Producers = media.MediaInfo.VideoProperties.Producers,
            Writers = media.MediaInfo.VideoProperties.Writers,
            Width = media.MediaInfo.VideoProperties.Width,
            Height = media.MediaInfo.VideoProperties.Height,
            VideoBitrate = media.MediaInfo.VideoProperties.Bitrate,
        };
    }

    private static MediaInfo CreateMediaInfo(RawMediaRecordDto record)
    {
        TimeSpan duration = TimeSpan.FromTicks(record.DurationTicks);
        if (record.MediaType == MediaPlaybackType.Music)
        {
            return new MediaInfo(new MusicInfo
            {
                Title = record.Title,
                Artist = record.Artist,
                Album = record.Album,
                AlbumArtist = record.AlbumArtist,
                Composers = record.Composers,
                Genre = record.Genre,
                TrackNumber = record.TrackNumber,
                Year = record.Year,
                Duration = duration,
                Bitrate = record.Bitrate,
            });
        }

        if (record.MediaType == MediaPlaybackType.Video)
        {
            return new MediaInfo(new VideoInfo
            {
                Title = record.Title,
                Subtitle = record.Subtitle,
                Producers = record.Producers,
                Writers = record.Writers,
                Year = record.Year,
                Duration = duration,
                Width = record.Width,
                Height = record.Height,
                Bitrate = record.VideoBitrate,
            });
        }

        return new MediaInfo(record.MediaType, record.Title, record.Year, duration);
    }

    private void UpdatePlaylist()
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await SaveAsync();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex);
                }
            });
        }
    }
}
