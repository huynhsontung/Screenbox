using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PropertyViewModel : ObservableObject
{
    public ObservableCollection<MediaMetadata> MediaProperties { get; }

    public ObservableCollection<MediaMetadata> VideoProperties { get; }

    public ObservableCollection<MediaMetadata> AudioProperties { get; }

    public ObservableCollection<MediaMetadata> FileProperties { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFileLocationCommand))]
    public partial bool CanNavigateToFile { get; set; }

    private readonly IFilesService _filesService;
    private readonly IMediaProbeService _mediaProbeService;
    private StorageFile? _mediaFile;
    private Uri? _mediaUri;

    public PropertyViewModel(IFilesService filesService, IMediaProbeService mediaProbeService)
    {
        _filesService = filesService;
        _mediaProbeService = mediaProbeService;
        MediaProperties = new ObservableCollection<MediaMetadata>();
        VideoProperties = new ObservableCollection<MediaMetadata>();
        AudioProperties = new ObservableCollection<MediaMetadata>();
        FileProperties = new ObservableCollection<MediaMetadata>();
    }

    /// <summary>
    /// Loads detailed media information for the given <paramref name="media"/> item when the view loads.
    /// </summary>
    public async void OnLoaded(MediaViewModel media)
    {
        await LoadPropertiesAsync(media);
    }

    /// <summary>
    /// Loads platform file details and probes media properties on demand for the specified <paramref name="media"/> item,
    /// refreshing the property collections as data becomes available.
    /// </summary>
    /// <param name="media">The media view model whose properties are being inspected.</param>
    public async Task LoadPropertiesAsync(MediaViewModel media)
    {
        if (!media.DetailsLoaded)
        {
            await media.LoadDetailsAsync(_filesService);
            UpdateProperties(media);
        }

        if (!media.IsMediaInfoParsed)
        {
            await media.ParseDetailsAsync(_mediaProbeService);
        }

        UpdateProperties(media);
    }

    /// <summary>
    /// Populates the property collections with metadata keys and values from the given <paramref name="media"/> item.
    /// </summary>
    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    public void UpdateProperties(MediaViewModel media)
    {
        List<MediaMetadata> mediaEntries = new();
        List<MediaMetadata> videoEntries = new();
        List<MediaMetadata> audioEntries = new();
        List<MediaMetadata> fileEntries = new();

        switch (media.MediaType)
        {
            case MediaPlaybackType.Video:
                mediaEntries.Add(new MediaMetadata(MediaProperty.Title, string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                    ? media.Name
                    : media.MediaInfo.VideoProperties.Title));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Subtitle, media.MediaInfo.VideoProperties.Subtitle));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Year, media.MediaInfo.VideoProperties.Year > 0
                    ? media.MediaInfo.VideoProperties.Year.ToString()
                    : string.Empty));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Producers, string.Join("; ", media.MediaInfo.VideoProperties.Producers)));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Writers, string.Join("; ", media.MediaInfo.VideoProperties.Writers)));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Length, Humanizer.ToDuration(media.MediaInfo.VideoProperties.Duration)));

                videoEntries.Add(new MediaMetadata(MediaProperty.Resolution, $"{media.MediaInfo.VideoProperties.Width}×{media.MediaInfo.VideoProperties.Height}"));
                videoEntries.Add(new MediaMetadata(MediaProperty.BitRate, $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps"));

                audioEntries.Add(new MediaMetadata(MediaProperty.BitRate, $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;

            case MediaPlaybackType.Music:
                mediaEntries.Add(new MediaMetadata(MediaProperty.Title, media.MediaInfo.MusicProperties.Title));
                mediaEntries.Add(new MediaMetadata(MediaProperty.ContributingArtists, media.MediaInfo.MusicProperties.Artist));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Album, media.MediaInfo.MusicProperties.Album));
                mediaEntries.Add(new MediaMetadata(MediaProperty.AlbumArtist, media.MediaInfo.MusicProperties.AlbumArtist));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Composers, string.Join("; ", media.MediaInfo.MusicProperties.Composers)));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Genre, string.Join("; ", media.MediaInfo.MusicProperties.Genre)));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Track, media.MediaInfo.MusicProperties.TrackNumber.ToString()));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Year, media.MediaInfo.MusicProperties.Year > 0
                    ? media.MediaInfo.MusicProperties.Year.ToString()
                    : string.Empty));
                mediaEntries.Add(new MediaMetadata(MediaProperty.Length, Humanizer.ToDuration(media.MediaInfo.MusicProperties.Duration)));

                audioEntries.Add(new MediaMetadata(MediaProperty.BitRate, $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;
        }

        switch (media.Source)
        {
            case StorageFile file:
                _mediaFile = file;
                _mediaUri = null;
                CanNavigateToFile = true;
                fileEntries.Add(new MediaMetadata(MediaProperty.FileType, _mediaFile.FileType));
                fileEntries.Add(new MediaMetadata(MediaProperty.ContentType, _mediaFile.ContentType));
                fileEntries.Add(new MediaMetadata(MediaProperty.Size, BytesToHumanReadable((long)media.MediaInfo.Size)));
                fileEntries.Add(new MediaMetadata(MediaProperty.LastModified, media.MediaInfo.DateModified.ToString()));
                break;
            case Uri uri:
                _mediaFile = null;
                _mediaUri = uri;
                CanNavigateToFile = uri.IsFile;
                break;
        }

        SyncCollection(MediaProperties, mediaEntries);
        SyncCollection(VideoProperties, videoEntries);
        SyncCollection(AudioProperties, audioEntries);
        SyncCollection(FileProperties, fileEntries);

        OnPropertyChanged(nameof(MediaProperties));
        OnPropertyChanged(nameof(VideoProperties));
        OnPropertyChanged(nameof(AudioProperties));
        OnPropertyChanged(nameof(FileProperties));
    }

    private static void SyncCollection(ObservableCollection<MediaMetadata> target, IReadOnlyList<MediaMetadata> source)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (i < target.Count)
            {
                if (target[i] != source[i])
                {
                    target[i] = source[i];
                }
            }
            else
            {
                target.Add(source[i]);
            }
        }

        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToFile))]
    private void OpenFileLocation()
    {
        if (_mediaFile != null)
            _filesService.OpenFileLocationAsync(_mediaFile);
        else if (_mediaUri != null)
            _filesService.OpenFileLocationAsync(_mediaUri.OriginalString);
    }

    // https://stackoverflow.com/a/11124118
    private static string BytesToHumanReadable(long byteCount)
    {
        // Get absolute value
        long absCount = byteCount < 0 ? -byteCount : byteCount;
        // Determine the suffix and readable value
        string suffix;
        double readable;
        if (absCount >= 0x1000000000000000) // Exabyte
        {
            suffix = "EB";
            readable = byteCount >> 50;
        }
        else if (absCount >= 0x4000000000000) // Petabyte
        {
            suffix = "PB";
            readable = byteCount >> 40;
        }
        else if (absCount >= 0x10000000000) // Terabyte
        {
            suffix = "TB";
            readable = byteCount >> 30;
        }
        else if (absCount >= 0x40000000) // Gigabyte
        {
            suffix = "GB";
            readable = byteCount >> 20;
        }
        else if (absCount >= 0x100000) // Megabyte
        {
            suffix = "MB";
            readable = byteCount >> 10;
        }
        else if (absCount >= 0x400) // Kilobyte
        {
            suffix = "KB";
            readable = byteCount;
        }
        else
        {
            return byteCount.ToString("0 B"); // Byte
        }
        // Divide by 1024 to get fractional value
        readable = readable / 1024;
        // Return formatted number with suffix
        return readable.ToString("0.## ") + suffix;
    }
}
