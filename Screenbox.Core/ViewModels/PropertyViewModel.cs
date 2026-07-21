#nullable enable

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PropertyViewModel : ObservableObject
{
    public IList<MediaMetadata> MediaProperties { get; }

    public IList<MediaMetadata> VideoProperties { get; }

    public IList<MediaMetadata> AudioProperties { get; }

    public IList<MediaMetadata> FileProperties { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFileLocationCommand))]
    private bool _canNavigateToFile;

    private readonly IFilesService _filesService;
    private StorageFile? _mediaFile;
    private Uri? _mediaUri;

    public PropertyViewModel(IFilesService filesService)
    {
        _filesService = filesService;
        MediaProperties = new List<MediaMetadata>();
        VideoProperties = new List<MediaMetadata>();
        AudioProperties = new List<MediaMetadata>();
        FileProperties = new List<MediaMetadata>();
    }

    /// <summary>
    /// Loads detailed media information for the given <paramref name="media"/> item when the view loads.
    /// </summary>
    public async void OnLoaded(MediaViewModel media)
    {
        await media.LoadDetailsAsync(_filesService);
    }

    /// <summary>
    /// Populates the property dictionaries with localized labels and values from the given <paramref name="media"/> item.
    /// </summary>
    public void UpdateProperties(MediaViewModel media)
    {
        switch (media.MediaType)
        {
            case MediaPlaybackType.Video:
                MediaProperties.Add(new MediaMetadata("PropertyTitle", string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                    ? media.Name
                    : media.MediaInfo.VideoProperties.Title));
                MediaProperties.Add(new MediaMetadata("PropertySubtitle", media.MediaInfo.VideoProperties.Subtitle));
                MediaProperties.Add(new MediaMetadata("PropertyYear", media.MediaInfo.VideoProperties.Year > 0
                    ? media.MediaInfo.VideoProperties.Year.ToString()
                    : string.Empty));
                MediaProperties.Add(new MediaMetadata("PropertyProducers", string.Join("; ", media.MediaInfo.VideoProperties.Producers)));
                MediaProperties.Add(new MediaMetadata("PropertyWriters", string.Join("; ", media.MediaInfo.VideoProperties.Writers)));
                MediaProperties.Add(new MediaMetadata("PropertyLength", Humanizer.ToDuration(media.MediaInfo.VideoProperties.Duration)));

                VideoProperties.Add(new MediaMetadata("PropertyResolution", $"{media.MediaInfo.VideoProperties.Width}×{media.MediaInfo.VideoProperties.Height}"));
                VideoProperties.Add(new MediaMetadata("PropertyBitRate", $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps"));

                AudioProperties.Add(new MediaMetadata("PropertyBitRate", $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;

            case MediaPlaybackType.Music:
                MediaProperties.Add(new MediaMetadata("PropertyTitle", media.MediaInfo.MusicProperties.Title));
                MediaProperties.Add(new MediaMetadata("PropertyContributingArtists", media.MediaInfo.MusicProperties.Artist));
                MediaProperties.Add(new MediaMetadata("PropertyAlbum", media.MediaInfo.MusicProperties.Album));
                MediaProperties.Add(new MediaMetadata("PropertyAlbumArtist", media.MediaInfo.MusicProperties.AlbumArtist));
                MediaProperties.Add(new MediaMetadata("PropertyComposers", string.Join("; ", media.MediaInfo.MusicProperties.Composers)));
                MediaProperties.Add(new MediaMetadata("PropertyGenre", string.Join("; ", media.MediaInfo.MusicProperties.Genre)));
                MediaProperties.Add(new MediaMetadata("PropertyTrack", media.MediaInfo.MusicProperties.TrackNumber.ToString()));
                MediaProperties.Add(new MediaMetadata("PropertyYear", media.MediaInfo.MusicProperties.Year > 0
                    ? media.MediaInfo.MusicProperties.Year.ToString()
                    : string.Empty));
                MediaProperties.Add(new MediaMetadata("PropertyLength", Humanizer.ToDuration(media.MediaInfo.MusicProperties.Duration)));

                AudioProperties.Add(new MediaMetadata("PropertyBitRate", $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;
        }

        switch (media.Source)
        {
            case StorageFile file:
                _mediaFile = file;
                CanNavigateToFile = true;
                FileProperties.Add(new MediaMetadata("PropertyFileType", _mediaFile.FileType));
                FileProperties.Add(new MediaMetadata("PropertyContentType", _mediaFile.ContentType));
                FileProperties.Add(new MediaMetadata("PropertySize", BytesToHumanReadable((long)media.MediaInfo.Size)));
                FileProperties.Add(new MediaMetadata("PropertyLastModified", media.MediaInfo.DateModified.ToString()));
                break;
            case Uri uri:
                _mediaUri = uri;
                CanNavigateToFile = uri.IsFile;
                break;
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
