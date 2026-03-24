using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaylistViewModel : ObservableRecipient,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    private const int ThumbnailCollageSize = 768;

    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }
    [ObservableProperty] public partial WriteableBitmap? Thumbnail { get; set; }
    [ObservableProperty] public partial DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public string Id => _id.ToString();

    private bool HasItems => Items.Count > 0;

    private Guid _id = Guid.NewGuid();

    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _playlistSaveTimer;
    private readonly ILogger<PlaylistViewModel> _logger;

    public PlaylistViewModel(
        IPlaylistService playlistService,
        MediaViewModelFactory mediaFactory,
        ILogger<PlaylistViewModel> logger)
        : this(playlistService, mediaFactory, logger, WeakReferenceMessenger.Default)
    {
    }

    public PlaylistViewModel(
        IPlaylistService playlistService,
        MediaViewModelFactory mediaFactory,
        ILogger<PlaylistViewModel> logger,
        IMessenger messenger)
        : base(messenger)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _playlistSaveTimer = _dispatcherQueue.CreateTimer();

        Items.CollectionChanged += Items_CollectionChanged;

        Messenger.Register<QueuePlaybackStateChangedMessage>(this);

        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(Thumbnail));

        _playlistSaveTimer.Debounce(async () =>
        {
            // Ensures the playlist is up to date when items are reordered.
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist playlist '{PlaylistId}'.", Id);
            }

            PlayCommand.NotifyCanExecuteChanged();
            PlayNextCommand.NotifyCanExecuteChanged();
            AddToQueueCommand.NotifyCanExecuteChanged();
        }, TimeSpan.FromMilliseconds(50));

        // TODO: Update the collage only when the first four items in the collection change.
        _ = UpdateThumbnailCollageAsync();
    }

    public QueueSession ToQueueSession()
    {
        return new QueueSession(0, Items)
        {
            Origin = QueueOrigin.ForPlaylist(this)
        };
    }

    public void Load(PlaylistRecordDto persistentPlaylist)
    {
        if (!Guid.TryParse(persistentPlaylist.Id, out _id)) return;
        Name = persistentPlaylist.DisplayName;
        LastUpdated = persistentPlaylist.LastUpdated;
        Items.Clear();

        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
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

    public void Receive(QueuePlaybackStateChangedMessage message)
    {
        UpdatePlaybackState(message.State);
    }

    private void UpdatePlaybackState(QueuePlaybackState state)
    {
        bool isActive = state.SessionId != Guid.Empty &&
                        state.CurrentItem is not null &&
                        (state.Origin?.Matches(RecentItemKind.Playlist, Id) ?? false);
        IsQueueActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void Play()
    {
        if (IsQueueActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(showBadge: false));
            return;
        }

        Messenger.Send(new SetQueueMessage(ToQueueSession(), true));
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void PlayNext()
    {
        Messenger.SendPlayNext(Items);
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void AddToQueue()
    {
        Messenger.SendAddToQueue(Items);
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
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistItemsAdded, title: Name, numericValue: items.Count));
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
        Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.PlaylistItemsAdded, title: Name, numericValue: items.Count));
    }

    private PlaylistRecordDto ToPersistentPlaylist()
    {
        return new PlaylistRecordDto
        {
            Id = _id.ToString(),
            DisplayName = Name,
            LastUpdated = LastUpdated,
            Items = Items.Select(RawMediaRecordDto.FromMediaViewModel).ToList()
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

            media.MediaInfo = record.ToMediaInfo();

            if (media.MediaType == MediaPlaybackType.Music)
            {
                // Placeholder album and artist for media list view.
                // This path can happen when playlist is loaded before the music library is loaded
                albumFactory.AddSong(media);
                artistFactory.AddSong(media);
                media.Album = albumFactory.SongsToAlbums[media];
                media.Artists = artistFactory.SongsToArtists[media].ToArray();
                media.Album.UpdateProperties();
            }

            if (record.DateAdded != default)
            {
                media.DateAdded = record.DateAdded.ToLocalTime();
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
