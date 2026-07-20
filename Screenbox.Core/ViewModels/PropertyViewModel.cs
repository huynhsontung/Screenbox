#nullable enable

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class PropertyViewModel : ObservableObject
{
    public Dictionary<string, string> MediaProperties { get; }

    public Dictionary<string, string> VideoProperties { get; }

    public Dictionary<string, string> AudioProperties { get; }

    public Dictionary<string, string> FileProperties { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFileLocationCommand))]
    private bool _canNavigateToFile;

    private readonly IFilesService _filesService;
    private StorageFile? _mediaFile;
    private Uri? _mediaUri;

    public PropertyViewModel(IFilesService filesService)
    {
        _filesService = filesService;
        MediaProperties = new Dictionary<string, string>();
        VideoProperties = new Dictionary<string, string>();
        AudioProperties = new Dictionary<string, string>();
        FileProperties = new Dictionary<string, string>();
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
                MediaProperties["PropertyTitle"] = string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                    ? media.Name
                    : media.MediaInfo.VideoProperties.Title;
                MediaProperties["PropertySubtitle"] = media.MediaInfo.VideoProperties.Subtitle;
                MediaProperties["PropertyYear"] = media.MediaInfo.VideoProperties.Year > 0
                    ? media.MediaInfo.VideoProperties.Year.ToString()
                    : string.Empty;
                MediaProperties["PropertyProducers"] = string.Join("; ", media.MediaInfo.VideoProperties.Producers);
                MediaProperties["PropertyWriters"] = string.Join("; ", media.MediaInfo.VideoProperties.Writers);
                MediaProperties["PropertyLength"] = Humanizer.ToDuration(media.MediaInfo.VideoProperties.Duration);

                VideoProperties["PropertyResolution"] = $"{media.MediaInfo.VideoProperties.Width}×{media.MediaInfo.VideoProperties.Height}";
                VideoProperties["PropertyBitRate"] = $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps";

                AudioProperties["PropertyBitRate"] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                break;

            case MediaPlaybackType.Music:
                MediaProperties["PropertyTitle"] = media.MediaInfo.MusicProperties.Title;
                MediaProperties["PropertyContributingArtists"] = media.MediaInfo.MusicProperties.Artist;
                MediaProperties["PropertyAlbum"] = media.MediaInfo.MusicProperties.Album;
                MediaProperties["PropertyAlbumArtist"] = media.MediaInfo.MusicProperties.AlbumArtist;
                MediaProperties["PropertyComposers"] = string.Join("; ", media.MediaInfo.MusicProperties.Composers);
                MediaProperties["PropertyGenre"] = string.Join("; ", media.MediaInfo.MusicProperties.Genre);
                MediaProperties["PropertyTrack"] = media.MediaInfo.MusicProperties.TrackNumber.ToString();
                MediaProperties["PropertyYear"] = media.MediaInfo.MusicProperties.Year > 0
                    ? media.MediaInfo.MusicProperties.Year.ToString()
                    : string.Empty;
                MediaProperties["PropertyLength"] = Humanizer.ToDuration(media.MediaInfo.MusicProperties.Duration);

                AudioProperties["PropertyBitRate"] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                break;
        }

        switch (media.Source)
        {
            case StorageFile file:
                _mediaFile = file;
                CanNavigateToFile = true;
                FileProperties["PropertyFileType"] = _mediaFile.FileType;
                FileProperties["PropertyContentType"] = _mediaFile.ContentType;
                FileProperties["PropertySize"] = BytesToHumanReadable((long)media.MediaInfo.Size);
                FileProperties["PropertyLastModified"] = media.MediaInfo.DateModified.ToString();
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
