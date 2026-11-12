using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlaylistViewModel : ObservableRecipient,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    private const int ThumbnailGridColumns = 2;
    private const int ThumbnailCellCount = ThumbnailGridColumns * ThumbnailGridColumns;
    private const int ThumbnailCollageSize = 600;
    private const int ThumbnailCellSize = ThumbnailCollageSize / ThumbnailGridColumns;
    private const int BytesPerPixel = 4;

    public ObservableCollection<MediaViewModel> Items { get; } = new();

    public double ItemsCount => Items.Count;    // For binding

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsQueueActive { get; set; }
    [ObservableProperty] public partial BitmapImage? Thumbnail { get; set; }
    [ObservableProperty] public partial DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    public string Id => _id.ToString();

    private bool HasItems => Items.Count > 0;

    private Guid _id = Guid.NewGuid();

    private bool _isLoading;
    private CancellationTokenSource? _collageCts;

    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _playlistSaveTimer;
    private readonly DispatcherQueueTimer _collageTimer;
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
        _collageTimer = _dispatcherQueue.CreateTimer();

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

        if (_isLoading)
            return;

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

        // The thumbnail collage only shows the first few items, so ignore changes that can't affect it.
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewStartingIndex < ThumbnailCellCount:
            case NotifyCollectionChangedAction.Remove when e.OldStartingIndex < ThumbnailCellCount:
            case NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move
                when e.NewStartingIndex < ThumbnailCellCount || e.OldStartingIndex < ThumbnailCellCount:
            case NotifyCollectionChangedAction.Reset:
                _collageTimer.Debounce(async () =>
                {
                    await UpdateThumbnailCollageAsync();
                }, TimeSpan.FromMilliseconds(50));
                break;
        }
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
        _isLoading = true;
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
        _isLoading = false;
    }

    /// <summary>
    /// Loads the playlist thumbnail, generating a collage if no saved thumbnail exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public async Task LoadThumbnailAsync()
    {
        if (Items.Count == 0 || Thumbnail is not null)
            return;

        try
        {
            StorageFile? file = await _playlistService.GetThumbnailFileAsync(Id);
            if (file is null)
            {
                await UpdateThumbnailCollageAsync();
                return;
            }

            using IRandomAccessStream stream = await file.OpenReadAsync();
            await SetThumbnailSourceAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load thumbnail for playlist {PlaylistId}.", Id);
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

    private async Task UpdateThumbnailCollageAsync()
    {
        _collageCts?.Cancel();
        _collageCts?.Dispose();

        CancellationTokenSource cts = new();
        _collageCts = cts;

        if (Items.Count == 0)
        {
            Thumbnail = null;
            await _playlistService.DeleteThumbnailAsync(Id);
            return;
        }

        try
        {
            byte[]? collagePixels = await BuildThumbnailCollageAsync(cts.Token);
            if (collagePixels is null)
            {
                Thumbnail = null;
                await _playlistService.DeleteThumbnailAsync(Id);
                return;
            }

            using InMemoryRandomAccessStream stream = await EncodeThumbnailAsync(collagePixels, cts.Token);

            await _playlistService.SaveThumbnailAsync(Id, stream);
            stream.Seek(0);
            await SetThumbnailSourceAsync(stream);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer update.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update thumbnail for playlist {PlaylistId}.", Id);
        }
    }

    private async Task<byte[]?> BuildThumbnailCollageAsync(CancellationToken cancellationToken)
    {
        MediaViewModel[] sources = Items.Take(ThumbnailCellCount).ToArray();

        byte[]?[] cells = await Task.WhenAll(
            sources.Select(item => DecodeThumbnailCellAsync(item, ThumbnailCellSize, cancellationToken)));

        cancellationToken.ThrowIfCancellationRequested();

        byte[] collage = new byte[ThumbnailCollageSize * ThumbnailCollageSize * BytesPerPixel];
        bool hasContent = false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] is not byte[] cell)
                continue;

            CopyCellIntoThumbnailCollage(collage, cell, i);
            hasContent = true;
        }

        return hasContent ? collage : null;
    }

    private static async Task<InMemoryRandomAccessStream> EncodeThumbnailAsync(
        byte[] pixels,
        CancellationToken cancellationToken)
    {
        InMemoryRandomAccessStream stream = new();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Straight,
            width: ThumbnailCollageSize,
            height: ThumbnailCollageSize,
            dpiX: 96.0,
            dpiY: 96.0,
            pixels);
        await encoder.FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();

        return stream;
    }

    private async Task SetThumbnailSourceAsync(IRandomAccessStream stream)
    {
        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(stream);
        Thumbnail = bitmapImage;
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

    private static async Task<byte[]?> DecodeThumbnailCellAsync(MediaViewModel item, int size, CancellationToken cancellationToken)
    {
        try
        {
            using IRandomAccessStream? stream = await item.GetThumbnailSourceAsync();
            if (stream is null)
                return null;

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            cancellationToken.ThrowIfCancellationRequested();

            if (decoder.PixelWidth == 0 || decoder.PixelHeight == 0)
                return null;

            // Scale so the shorter side fills the cell, then crop the overflow from the center.
            uint cellSize = (uint)size;
            double scale = Math.Max((double)cellSize / decoder.PixelWidth, (double)cellSize / decoder.PixelHeight);
            uint scaledWidth = Math.Max(cellSize, (uint)Math.Round(decoder.PixelWidth * scale));
            uint scaledHeight = Math.Max(cellSize, (uint)Math.Round(decoder.PixelHeight * scale));

            BitmapTransform transform = new()
            {
                ScaledWidth = scaledWidth,
                ScaledHeight = scaledHeight,
                Bounds = new BitmapBounds
                {
                    X = (scaledWidth - cellSize) / 2,
                    Y = (scaledHeight - cellSize) / 2,
                    Width = cellSize,
                    Height = cellSize,
                },
                InterpolationMode = BitmapInterpolationMode.Fant,
            };

            PixelDataProvider pixelProvider = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);

            return pixelProvider.DetachPixelData();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static void CopyCellIntoThumbnailCollage(byte[] collage, byte[] cell, int index)
    {
        int bytesPerRow = ThumbnailCellSize * BytesPerPixel;
        int column = index % ThumbnailGridColumns;
        int row = index / ThumbnailGridColumns;
        int x = column * ThumbnailCellSize;
        int y = row * ThumbnailCellSize;

        for (int cellRow = 0; cellRow < ThumbnailCellSize; cellRow++)
        {
            int destinationOffset = ((y + cellRow) * ThumbnailCollageSize + x) * BytesPerPixel;
            cell.AsSpan(cellRow * bytesPerRow, bytesPerRow)
                .CopyTo(collage.AsSpan(destinationOffset, bytesPerRow));
        }
    }
}
