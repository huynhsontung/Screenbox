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
    private readonly DispatcherQueue? _dispatcherQueue;

    public PlaylistViewModel(IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Items.CollectionChanged += Items_OnCollectionChanged;
    }

    private void Items_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(Thumbnail));

        // TODO: Update the collage only when the first four items in the collection change.
        _ = UpdateThumbnailCollageAsync();
    }

    public Playlist ToPlaylist()
    {
        return new Playlist(0, Items);
    }

    public void Load(PersistentPlaylist persistentPlaylist)
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

        await SaveAsync();
        Messenger.Send(new PlaylistItemsAddedNotificationMessage(Name, items.Count));
    }

    private PersistentPlaylist ToPersistentPlaylist()
    {
        return new PersistentPlaylist
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(m => new PersistentMediaRecord(
                m.Name,
                m.Location,
                m.MediaType == MediaPlaybackType.Music ? m.MediaInfo.MusicProperties : m.MediaInfo.VideoProperties,
                m.DateAdded
            )).ToList()
        };
    }

    private MediaViewModel ToMediaViewModel(PersistentMediaRecord record)
    {
        MediaViewModel media;
        bool existing = false;
        if (Uri.TryCreate(record.Path, UriKind.Absolute, out var uri))
        {
            if (_mediaFactory.TryGetSingleton(uri, out var existingMedia))
            {
                media = existingMedia!;
                existing = true;
            }
            else
            {
                media = _mediaFactory.GetSingleton(uri);
            }
        }
        else
        {
            media = _mediaFactory.GetTransient(new Uri("about:blank"));
            media.IsAvailable = false;
        }

        if (!existing)
        {
            if (!string.IsNullOrEmpty(record.Title))
                media.Name = record.Title;

            media.MediaInfo = record.Properties != null
                ? new MediaInfo(record.Properties)
                : new MediaInfo(record.MediaType, record.Title, record.Year, record.Duration);

            if (record.DateAdded != default)
            {
                DateTimeOffset utcTime = DateTime.SpecifyKind(record.DateAdded, DateTimeKind.Utc);
                media.DateAdded = utcTime.ToLocalTime();
            }
        }

        return media;
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
