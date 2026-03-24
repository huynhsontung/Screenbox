#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public partial class PlaylistViewModel : ObservableRecipient
{
    private const int ThumbnailCollageSize = 768;

    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private WriteableBitmap? _thumbnail;
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
        OnPropertyChanged(nameof(Thumbnail));

        // Ensures the playlist is up to date when items are reordered, as the ListViewBase
        // reorder operation sends Remove/Add actions instead of a Move action.
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            _playlistSaveTimer.Debounce(() => UpdatePlaylist(), TimeSpan.FromMilliseconds(100));
        }

        // TODO: Update the collage only when the first four items in the collection change.
        _ = UpdateThumbnailCollageAsync();
    }

    public Playlist ToPlaylist()
    {
        return new Playlist(0, Items);
    }

    public void Load(PlaylistRecordDto persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();
        var itemSet = new HashSet<MediaViewModel>(persistentPlaylist.Items.Count);

        var albumFactory = new AlbumViewModelFactory();
        var artistFactory = new ArtistViewModelFactory();

        foreach (var item in persistentPlaylist.Items)
        {
            try
            {
                var vm = ToMediaViewModel(item, albumFactory, artistFactory);
                Items.Add(itemSet.Add(vm) ? vm : new MediaViewModel(vm));
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

    private PlaylistRecordDto ToPersistentPlaylist()
    {
        return new PlaylistRecordDto
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(ToRawMediaRecord).ToList()
        };
    }

    private MediaViewModel ToMediaViewModel(RawMediaRecordDto record, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
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

            if (media.MediaType == MediaPlaybackType.Music)
            {
                // Placeholder album and artist for media list view.
                // This path can happen when playlist is loaded before the music library is loaded
                albumFactory.AddSong(media);
                artistFactory.AddSong(media);
                media.Album = albumFactory.SongsToAlbums[media];
                media.Artists = artistFactory.SongsToArtists[media].ToArray();
            }

            if (record.DateAdded != default)
            {
                media.DateAdded = record.DateAdded.ToLocalTime();
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
            DateAdded = media.DateAdded,
            Duration = media.Duration,
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
                Duration = record.Duration,
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
                Duration = record.Duration,
                Width = record.Width,
                Height = record.Height,
                Bitrate = record.VideoBitrate,
            });
        }

        return new MediaInfo(record.MediaType, record.Title, record.Year, record.Duration);
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

    private async Task UpdateThumbnailCollageAsync()
    {
        if (ItemsCount == 0)
        {
            Thumbnail = null;
            return;
        }

        try
        {
            var sources = new List<IRandomAccessStream?>();

            foreach (var item in Items.Take(4))
            {
                try
                {
                    var stream = await item.GetThumbnailSourceAsync();
                    sources.Add(stream);
                }
                catch
                {
                    sources.Add(null);
                }
            }

            var bitmap = await CombineStreamsToWriteableBitmapAsync(sources);
            if (bitmap is null) return;

            if (_dispatcherQueue is not null)
            {
                _dispatcherQueue.TryEnqueue(() => Thumbnail = bitmap);
            }
        }
        catch
        {
        }
    }

    private static async Task<WriteableBitmap?> CombineStreamsToWriteableBitmapAsync(IReadOnlyList<IRandomAccessStream?> sources)
    {
        const int Bpp = 4;

        int width = ThumbnailCollageSize;
        int height = ThumbnailCollageSize;

        byte[] result = new byte[width * height * Bpp];

        // Cell size for a 2x2 grid.
        int cellWidth = width / 2;
        int cellHeight = height / 2;

        for (int i = 0; i < Math.Min(4, sources.Count); i++)
        {
            var stream = sources[i];

            if (stream is null) continue;

            try
            {
                stream.Seek(0);
                var decoder = await BitmapDecoder.CreateAsync(stream);

                var transform = new BitmapTransform
                {
                    ScaledWidth = (uint)cellWidth,
                    ScaledHeight = (uint)cellHeight,
                    InterpolationMode = BitmapInterpolationMode.Fant,
                };

                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                byte[] sourcePixels = pixelData.DetachPixelData();

                int sourceStride = cellWidth * Bpp;

                int col = i % 2;
                int row = i / 2;
                int offsetX = col * cellWidth;
                int offsetY = row * cellHeight;

                for (int y = 0; y < cellHeight; y++)
                {
                    int sourceIndex = y * sourceStride;
                    int destinationIndex = ((offsetY + y) * width + offsetX) * Bpp;
                    sourcePixels.AsSpan(sourceIndex, sourceStride).CopyTo(result.AsSpan(destinationIndex, sourceStride));
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        try
        {
            var writableBitmap = new WriteableBitmap(width, height);
            using (var stream = writableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(result, 0, result.Length);
            }

            return writableBitmap;
        }
        catch
        {
            return null;
        }
    }
}
