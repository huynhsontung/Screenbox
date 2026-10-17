using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class MediaViewModel : ObservableRecipient
{
    public string Location { get; }

    public object Source { get; private set; }

    public bool IsFromLibrary { get; set; }

    public bool DetailsLoaded { get; private set; }

    public ArtistViewModel? MainArtist => Artists.FirstOrDefault();

    public Lazy<PlaybackItem?> Item { get; internal set; }

    public IReadOnlyList<string> Options { get; }

    public DateTimeOffset DateAdded { get; set; }

    public MediaPlaybackType MediaType => MediaInfo.MediaType;

    public TimeSpan Duration => MediaInfo.MusicProperties.Duration > TimeSpan.Zero
        ? MediaInfo.MusicProperties.Duration
        : MediaInfo.VideoProperties.Duration;

    public string TrackNumberText =>
        MediaInfo.MusicProperties.TrackNumber > 0 ? MediaInfo.MusicProperties.TrackNumber.ToString() : string.Empty;    // Helper for binding

    public BitmapImage? Thumbnail
    {
        get
        {
            if (_thumbnailRef == null) return null;
            return _thumbnailRef.TryGetTarget(out BitmapImage? image) ? image : null;
        }
        set
        {
            if (_thumbnailRef == null && value == null) return;
            if ((_thumbnailRef?.TryGetTarget(out BitmapImage? image) ?? false) && image == value) return;
            SetProperty(ref _thumbnailRef, value == null ? null : new WeakReference<BitmapImage>(value));
        }
    }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly IPlayerService _playerService;
    private readonly PlayerContext _playerContext;
    private readonly List<string> _options;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsMediaActive { get; set; }
    [ObservableProperty] public partial bool IsAvailable { get; set; } = true;
    [ObservableProperty] public partial AlbumViewModel? Album { get; set; }
    [ObservableProperty] public partial string Caption { get; set; } = string.Empty;  // For list item subtitle
    [ObservableProperty] public partial string AltCaption { get; set; } = string.Empty;   // For player page subtitle

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackNumberText))]
    [NotifyPropertyChangedFor(nameof(Duration))]
    [NotifyPropertyChangedFor(nameof(MediaType))]
    public partial MediaInfo MediaInfo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainArtist))]
    public partial ArtistViewModel[] Artists { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    private WeakReference<BitmapImage>? _thumbnailRef;

    public MediaViewModel(MediaViewModel source)
    {
        _playerService = source._playerService;
        _playerContext = source._playerContext;
        Name = source.Name;
        _thumbnailRef = source._thumbnailRef;
        MediaInfo = source.MediaInfo;
        Artists = source.Artists;
        Album = source.Album;
        Caption = source.Caption;
        AltCaption = source.AltCaption;
        _options = new List<string>(source.Options);
        Options = new ReadOnlyCollection<string>(_options);
        Location = source.Location;
        Source = source.Source;
        Item = new Lazy<PlaybackItem?>(CreatePlaybackItem);
        DateAdded = source.DateAdded;
        IsFromLibrary = source.IsFromLibrary;
    }

    private MediaViewModel(object source, MediaInfo mediaInfo, PlayerContext playerContext, IPlayerService playerService)
    {
        _playerService = playerService;
        _playerContext = playerContext;
        Source = source;
        Location = string.Empty;
        DateAdded = DateTimeOffset.Now;
        Name = string.Empty;
        MediaInfo = mediaInfo;
        Artists = Array.Empty<ArtistViewModel>();
        _options = new List<string>();
        Options = new ReadOnlyCollection<string>(_options);
        Item = new Lazy<PlaybackItem?>(CreatePlaybackItem);
    }

    public MediaViewModel(PlayerContext playerContext, IPlayerService playerService, StorageFile file)
        : this(file, new MediaInfo(FilesHelpers.GetMediaTypeForFile(file)), playerContext, playerService)
    {
        Location = file.Path;
        Name = file.Name;
        AltCaption = file.Name;
    }

    public MediaViewModel(PlayerContext playerContext, IPlayerService playerService, Uri uri)
        : this(uri, new MediaInfo(MediaPlaybackType.Unknown), playerContext, playerService)
    {
        Guard.IsTrue(uri.IsAbsoluteUri);
        Location = uri.OriginalString;
        Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
    }

    public MediaViewModel(PlayerContext playerContext, IPlayerService playerService, Media media)
        : this(media, new MediaInfo(MediaPlaybackType.Unknown), playerContext, playerService)
    {
        Location = media.Mrl;

        // Media is already loaded, create PlaybackItem
        Item = new Lazy<PlaybackItem?>(new PlaybackItem(media, media));
    }

    partial void OnMediaInfoChanged(MediaInfo value)
    {
        UpdateCaptions();
    }

    private PlaybackItem? CreatePlaybackItem()
    {
        if (MediaPlayer == null)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"Media player is not initialized{Environment.NewLine}{Location}"));
            return null;
        }

        PlaybackItem? item = null;
        try
        {
            if (Source is Media mediaSource)
            {
                item = new PlaybackItem(mediaSource, mediaSource);
            }
            else
            {
                item = _playerService.CreatePlaybackItem(MediaPlayer, Source, _options.ToArray());
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // Coding error. Rethrow.
            throw;
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.MediaLoadFailed, message: $"{Location}{Environment.NewLine}{e.Message}"));
        }

        return item;
    }

    public override string ToString()
    {
        return $"{Name}; {Caption}";
    }

    public void SetOptions(string options)
    {
        string[] opts = options.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(o => o.StartsWith(":") && o.Length > 1).ToArray();

        // Check if new options and existing options are the same
        if (opts.Length == _options.Count)
        {
            bool same = !opts.Where((o, i) => o != _options[i]).Any();
            if (same) return;
        }

        _options.Clear();
        _options.AddRange(opts);

        if (!Item.IsValueCreated) return;
        Clean();
    }

    public void Clean()
    {
        // If source is Media then there is no way to recreate. Don't clean up.
        if (Source is Media || !Item.IsValueCreated) return;
        PlaybackItem? item = Item.Value;
        Item = new Lazy<PlaybackItem?>(CreatePlaybackItem);
        if (item == null) return;
        _playerService.DisposePlaybackItem(item);
    }

    public void UpdateSource(StorageFile file)
    {
        Source = file;
        AltCaption = file.Name;
    }

    /// <summary>
    /// Loads basic and extended file properties using the platform file service for fast indexing.
    /// </summary>
    /// <param name="filesService">The platform file service.</param>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task LoadDetailsAsync(IFilesService filesService)
    {
        DetailsLoaded = true;
        MediaInfo? winrtInfo = null;
        switch (Source)
        {
            case StorageFile file:
                winrtInfo = await filesService.GetMediaInfoAsync(file);
                break;
            case Uri uri when await TryGetStorageFileFromUri(uri) is { } uriFile:
                UpdateSource(uriFile);
                winrtInfo = await filesService.GetMediaInfoAsync(uriFile);
                break;
        }

        if (winrtInfo is not null)
        {
            MediaInfo = MediaInfo.Merge(winrtInfo, MediaInfo);
        }
    }

    /// <summary>
    /// Probes the media source using <paramref name="mediaProbeService"/> and merges the parsed properties into <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="mediaProbeService">The media probe service used to parse the media.</param>
    /// <param name="cancellationToken">A token to cancel the probe operation.</param>
    public async Task LoadDetailsAsync(IMediaProbeService mediaProbeService, CancellationToken cancellationToken = default)
    {
        if (Source is Uri uri && await TryGetStorageFileFromUri(uri) is { } uriFile)
        {
            UpdateSource(uriFile);
        }

        object probeSource = Item is { IsValueCreated: true, Value: { } activeItem }
            ? activeItem
            : Source;
        MediaInfo? parsedInfo = await mediaProbeService.ProbeMediaAsync(
            probeSource,
            _options.Count > 0 ? _options.ToArray() : null,
            cancellationToken);

        if (parsedInfo is not null)
        {
            MediaInfo = MediaInfo.Merge(MediaInfo, parsedInfo);
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public async Task LoadThumbnailAsync()
    {
        if (Thumbnail != null) return;
        if (Source is Uri uri && await TryGetStorageFileFromUri(uri) is { } storageFile)
        {
            UpdateSource(storageFile);
        }

        if (Source is StorageFile file)
        {
            using var source = await GetThumbnailSourceAsync(file);
            if (source == null) return;
            BitmapImage image = new()
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = 300
            };

            try
            {
                await image.SetSourceAsync(source);
            }
            catch (Exception)
            {
                // WinRT component not found exception???
                return;
            }

            Thumbnail = image;
        }
        else if (Item is { IsValueCreated: true, Value.Media: { } media } &&
                 media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
                 Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri? artworkUri))
        {
            Thumbnail = new BitmapImage(artworkUri)
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = 300
            };
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public Task<IRandomAccessStream?> GetThumbnailSourceAsync()
    {
        return Source is not StorageFile file
            ? Task.FromResult<IRandomAccessStream?>(null)
            : GetThumbnailSourceAsync(file);
    }

    private static async Task<IRandomAccessStream?> GetThumbnailSourceAsync(StorageFile file)
    {
        if (!file.IsAvailable)
            return null;

        try
        {
            // Use SingleItem mode to retrieve embedded album art with original aspect ratio.
            // https://learn.microsoft.com/windows/apps/develop/files/thumbnails
            StorageItemThumbnail? source =
                await file.GetThumbnailAsync(ThumbnailMode.SingleItem, requestedSize: 1280, ThumbnailOptions.UseCurrentScale);
            if (source is { Type: ThumbnailType.Image })
            {
                return source;
            }
        }
        catch (Exception)
        {
            //// System.Exception: The data necessary to complete this operation is not yet available.
            //if (e.HResult != unchecked((int)0x8000000A) &&
            //    // System.Exception: The RPC server is unavailable.
            //    e.HResult != unchecked((int)0x800706BA))
            //    _logger.LogError(e, "Failed to load the image thumbnail.");
        }

        return null;
    }

    private void UpdateCaptions()
    {
        string title = MediaType switch
        {
            MediaPlaybackType.Video => MediaInfo.VideoProperties.Title,
            _ => !string.IsNullOrEmpty(MediaInfo.MusicProperties.Title)
                ? MediaInfo.MusicProperties.Title
                : MediaInfo.VideoProperties.Title
        };

        if (!string.IsNullOrEmpty(title))
        {
            Name = title;
        }

        if (Duration > TimeSpan.Zero)
        {
            Caption = Humanizer.ToDuration(Duration);
        }

        MusicInfo musicProperties = MediaInfo.MusicProperties;
        if (!string.IsNullOrEmpty(musicProperties.Artist))
        {
            Caption = musicProperties.Artist;
            AltCaption = string.IsNullOrEmpty(musicProperties.Album)
                ? musicProperties.Artist
                : $"{musicProperties.Artist} – {musicProperties.Album}";
        }
        else if (!string.IsNullOrEmpty(musicProperties.Album))
        {
            AltCaption = musicProperties.Album;
        }

        if (Name == AltCaption)
        {
            AltCaption = string.Empty;
        }
    }

    private static async Task<StorageFile?> TryGetStorageFileFromUri(Uri uri)
    {
        if (uri is { IsFile: true, IsLoopback: true, IsAbsoluteUri: true })
        {
            return await FilesHelpers.TryGetFileFromPathAsync(uri.OriginalString);
        }

        return null;
    }
}
