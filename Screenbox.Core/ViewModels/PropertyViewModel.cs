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
    /// Populates the property collections with metadata keys and values from the given <paramref name="media"/> item.
    /// </summary>
    public void UpdateProperties(MediaViewModel media)
    {
        switch (media.MediaType)
        {
            case MediaPlaybackType.Video:
                MediaProperties.Add(new MediaMetadata(Property.Title, string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                    ? media.Name
                    : media.MediaInfo.VideoProperties.Title));
                MediaProperties.Add(new MediaMetadata(Property.Subtitle, media.MediaInfo.VideoProperties.Subtitle));
                MediaProperties.Add(new MediaMetadata(Property.Year, media.MediaInfo.VideoProperties.Year > 0
                    ? media.MediaInfo.VideoProperties.Year.ToString()
                    : string.Empty));
                MediaProperties.Add(new MediaMetadata(Property.Producers, string.Join("; ", media.MediaInfo.VideoProperties.Producers)));
                MediaProperties.Add(new MediaMetadata(Property.Writers, string.Join("; ", media.MediaInfo.VideoProperties.Writers)));
                MediaProperties.Add(new MediaMetadata(Property.Length, Humanizer.ToDuration(media.MediaInfo.VideoProperties.Duration)));

                VideoProperties.Add(new MediaMetadata(Property.Resolution, $"{media.MediaInfo.VideoProperties.Width}×{media.MediaInfo.VideoProperties.Height}"));
                VideoProperties.Add(new MediaMetadata(Property.BitRate, $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps"));

                AudioProperties.Add(new MediaMetadata(Property.BitRate, $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;

            case MediaPlaybackType.Music:
                MediaProperties.Add(new MediaMetadata(Property.Title, media.MediaInfo.MusicProperties.Title));
                MediaProperties.Add(new MediaMetadata(Property.ContributingArtists, media.MediaInfo.MusicProperties.Artist));
                MediaProperties.Add(new MediaMetadata(Property.Album, media.MediaInfo.MusicProperties.Album));
                MediaProperties.Add(new MediaMetadata(Property.AlbumArtist, media.MediaInfo.MusicProperties.AlbumArtist));
                MediaProperties.Add(new MediaMetadata(Property.Composers, string.Join("; ", media.MediaInfo.MusicProperties.Composers)));
                MediaProperties.Add(new MediaMetadata(Property.Genre, string.Join("; ", media.MediaInfo.MusicProperties.Genre)));
                MediaProperties.Add(new MediaMetadata(Property.Track, media.MediaInfo.MusicProperties.TrackNumber.ToString()));
                MediaProperties.Add(new MediaMetadata(Property.Year, media.MediaInfo.MusicProperties.Year > 0
                    ? media.MediaInfo.MusicProperties.Year.ToString()
                    : string.Empty));
                MediaProperties.Add(new MediaMetadata(Property.Length, Humanizer.ToDuration(media.MediaInfo.MusicProperties.Duration)));

                AudioProperties.Add(new MediaMetadata(Property.BitRate, $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps"));
                break;
        }

        switch (media.Source)
        {
            case StorageFile file:
                _mediaFile = file;
                CanNavigateToFile = true;
                FileProperties.Add(new MediaMetadata(Property.FileType, _mediaFile.FileType));
                FileProperties.Add(new MediaMetadata(Property.ContentType, _mediaFile.ContentType));
                FileProperties.Add(new MediaMetadata(Property.Size, BytesToHumanReadable((long)media.MediaInfo.Size)));
                FileProperties.Add(new MediaMetadata(Property.LastModified, media.MediaInfo.DateModified.ToString()));
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
