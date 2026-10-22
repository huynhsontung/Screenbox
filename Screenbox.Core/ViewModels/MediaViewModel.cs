using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class MediaViewModel : ObservableRecipient, IPlaybackItem, ISelectable
{
    public string Location => Source.Location;

    public IMediaSource Source { get; }

    public bool IsFromLibrary { get; set; }

    public bool DetailsLoaded { get; private set; }

    public ArtistViewModel? MainArtist => Artists.FirstOrDefault();

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

    private readonly List<string> _options;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsMediaActive { get; set; }
    [ObservableProperty] public partial bool IsAvailable { get; set; } = true;
    [ObservableProperty] public partial AlbumViewModel? Album { get; set; }
    [ObservableProperty] public partial string Caption { get; set; } = string.Empty;  // For list item subtitle
    [ObservableProperty] public partial string AltCaption { get; set; } = string.Empty;   // For player page subtitle

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackNumberText))]
    public partial MediaInfo MediaInfo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainArtist))]
    public partial ArtistViewModel[] Artists { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    private WeakReference<BitmapImage>? _thumbnailRef;

    public MediaViewModel(MediaViewModel source)
    {
        Name = source.Name;
        _thumbnailRef = source._thumbnailRef;
        MediaInfo = source.MediaInfo;
        Artists = source.Artists;
        Album = source.Album;
        Caption = source.Caption;
        AltCaption = source.AltCaption;
        _options = new List<string>(source.Options);
        Options = new ReadOnlyCollection<string>(_options);
        Source = source.Source;
        DateAdded = source.DateAdded;
        IsFromLibrary = source.IsFromLibrary;
        DetailsLoaded = source.DetailsLoaded;
    }

    public MediaViewModel(IMediaSource source, MediaInfo? mediaInfo = null)
    {
        Source = source;
        DateAdded = DateTimeOffset.Now;
        Name = source.Name;
        MediaInfo = mediaInfo ?? new MediaInfo(source.MediaType);
        Artists = Array.Empty<ArtistViewModel>();
        _options = new List<string>();
        Options = new ReadOnlyCollection<string>(_options);
        AltCaption = source.Name;
    }

    partial void OnMediaInfoChanged(MediaInfo value)
    {
        UpdateCaptions();
    }

    public override string ToString()
    {
        return $"{Name}; {Caption}";
    }

    public void SetOptions(string options)
    {
        string[] opts = options.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(o => o.StartsWith(':') && o.Length > 1).ToArray();

        // Check if new options and existing options are the same
        if (opts.Length == _options.Count && opts.SequenceEqual(_options))
        {
            return;
        }

        _options.Clear();
        _options.AddRange(opts);
    }

    public void UpdateSource(StorageFile file)
    {
        if (Source is IFileMediaSource fileSource)
        {
            fileSource.UpdateStorageFile(file);
        }
        AltCaption = file.Name;
    }

    public async Task LoadDetailsAsync(IFilesService filesService)
    {
        DetailsLoaded = true;
        if (Source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
        {
            MediaInfo = await filesService.GetMediaInfoAsync(file);
        }

        switch (MediaType)
        {
            // TODO: Rework engine-discovered metadata fetching
            // case MediaPlaybackType.Unknown ...
            case MediaPlaybackType.Music when !string.IsNullOrEmpty(MediaInfo.MusicProperties.Title):
                Name = MediaInfo.MusicProperties.Title;
                break;
            case MediaPlaybackType.Video when !string.IsNullOrEmpty(MediaInfo.VideoProperties.Title):
                Name = MediaInfo.VideoProperties.Title;
                break;
        }

        // TODO: Rework engine-discovered metadata fetching
        // if (Item is { IsValueCreated: true, Value.Media: { IsParsed: true } media })
        // {
        //     if (Source is not IStorageItem &&
        //         media.Meta(MetadataType.Title) is { } title &&
        //         !string.IsNullOrEmpty(title) &&
        //         !Guid.TryParse(title, out Guid _))
        //     {
        //         Name = title;
        //     }
        //
        //     VideoInfo videoProperties = MediaInfo.VideoProperties;
        //     videoProperties.ShowName = media.Meta(MetadataType.ShowName) ?? videoProperties.ShowName;
        //     videoProperties.Season = media.Meta(MetadataType.Season) ?? videoProperties.Season;
        //     videoProperties.Episode = media.Meta(MetadataType.Episode) ?? videoProperties.Episode;
        // }

        if (Name == AltCaption)
            AltCaption = string.Empty;
    }

    public async Task LoadThumbnailAsync()
    {
        if (Thumbnail != null) return;

        if (Source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
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
        // TODO: Rework engine-discovered metadata fetching
        // else if (Item is { IsValueCreated: true, Value.Media: { } media } &&
        //          media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
        //          Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri? artworkUri))
        // {
        //     Thumbnail = new BitmapImage(artworkUri)
        //     {
        //         DecodePixelType = DecodePixelType.Logical,
        //         DecodePixelHeight = 300
        //     };
        // }
    }

    public Task<IRandomAccessStream?> GetThumbnailSourceAsync()
    {
        return Source is IFileMediaSource fileSource && fileSource.StorageFile is { } file
            ? GetThumbnailSourceAsync(file)
            : GetThumbnailSourceLazyAsync();
    }

    private async Task<IRandomAccessStream?> GetThumbnailSourceLazyAsync()
    {
        if (Source is IFileMediaSource fileSource && await fileSource.GetStorageFileAsync() is { } file)
        {
            return await GetThumbnailSourceAsync(file);
        }

        return null;
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

        // TODO: Rework engine-discovered metadata fetching
        // if (Item is { IsValueCreated: true, Value.Media: { IsParsed: true } media })
        // {
        //     string artist = media.Meta(MetadataType.Artist) ?? string.Empty;
        //     if (!string.IsNullOrEmpty(artist))
        //     {
        //         Caption = artist;
        //     }
        //
        //     if (media.Meta(MetadataType.Album) is { } album && !string.IsNullOrEmpty(album))
        //     {
        //         AltCaption = string.IsNullOrEmpty(artist) ? album : $"{artist} – {album}";
        //     }
        // }
    }
}
