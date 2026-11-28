using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;

public sealed partial class MediaViewModel : ObservableRecipient, IMediaPlaybackState
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
    [NotifyPropertyChangedFor(nameof(Duration))]
    [NotifyPropertyChangedFor(nameof(MediaType))]
    public partial MediaInfo MediaInfo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainArtist))]
    public partial ArtistViewModel[] Artists { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    private WeakReference<BitmapImage>? _thumbnailRef;

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
        string[] opts =         VlcArgumentHelper.ParseArguments(options)
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

    /// <summary>
    /// Loads media metadata from <see cref="Source"/> using <paramref name="metadataService"/> and updates <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="metadataService">The source metadata service used to extract media metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task LoadDetailsAsync(ISourceMetadataService metadataService, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadataService);
        DetailsLoaded = true;

        if (await metadataService.GetMediaInfoAsync(Source, cancellationToken) is { } parsedInfo)
        {
            ApplyMediaInfo(parsedInfo);
        }
    }

    /// <summary>
    /// Loads media metadata from <paramref name="item"/> using <paramref name="metadataService"/> and updates <see cref="MediaInfo"/>.
    /// </summary>
    /// <param name="metadataService">The playback metadata service used to extract media metadata.</param>
    /// <param name="item">The playback item to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task LoadDetailsAsync(IPlaybackMetadataService metadataService, IPlaybackItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadataService);
        ArgumentNullException.ThrowIfNull(item);

        if (await metadataService.GetMediaInfoAsync(item, cancellationToken) is { } parsedInfo)
        {
            if (Source is not IFileMediaSource)
            {
                DetailsLoaded = true;
            }

            ApplyMediaInfo(parsedInfo);
        }
    }

    private void ApplyMediaInfo(MediaInfo parsedInfo)
    {
        if (MediaType != MediaPlaybackType.Unknown)
        {
            parsedInfo.MediaType = MediaType;
        }
        else if (Source.MediaType != MediaPlaybackType.Unknown)
        {
            parsedInfo.MediaType = Source.MediaType;
        }

        MediaInfo = MediaInfo.MergeSelective(parsedInfo);
    }

    /// <summary>
    /// Loads the UI thumbnail for this media item using <paramref name="thumbnailService"/>.
    /// </summary>
    /// <param name="thumbnailService">The thumbnail service used to extract thumbnail streams.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task LoadThumbnailAsync(IMediaThumbnailService thumbnailService, CancellationToken cancellationToken = default)
    {
        if (Thumbnail is not null) return;

        if (await thumbnailService.GetThumbnailStreamAsync(Source, cancellationToken: cancellationToken) is { } source)
        {
            using (source)
            {
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
        }
        else if (MediaInfo.ArtworkUri is { } artworkUri)
        {
            Thumbnail = new BitmapImage(artworkUri)
            {
                DecodePixelType = DecodePixelType.Logical,
                DecodePixelHeight = 300
            };
        }
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
}
