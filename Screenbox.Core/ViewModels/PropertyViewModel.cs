#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using Windows.Media;
using Windows.Storage;

namespace Screenbox.Core.ViewModels
{
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
        private readonly IResourceService _resourceService;
        private StorageFile? _mediaFile;
        private Uri? _mediaUri;

        public PropertyViewModel(IFilesService filesService, IResourceService resourceService)
        {
            _filesService = filesService;
            _resourceService = resourceService;
            MediaProperties = new Dictionary<string, string>();
            VideoProperties = new Dictionary<string, string>();
            AudioProperties = new Dictionary<string, string>();
            FileProperties = new Dictionary<string, string>();
        }

        public void UpdateProperties(MediaViewModel media)
        {
            switch (media.MediaType)
            {
                case MediaPlaybackType.Video:
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTitle)] = string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                        ? media.Name
                        : media.MediaInfo.VideoProperties.Title;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertySubtitle)] = media.MediaInfo.VideoProperties.Subtitle;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyYear)] = media.MediaInfo.VideoProperties.Year > 0
                        ? media.MediaInfo.VideoProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyProducers)] = string.Join("; ", media.MediaInfo.VideoProperties.Producers);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyWriters)] = string.Join("; ", media.MediaInfo.VideoProperties.Writers);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration(media.MediaInfo.VideoProperties.Duration);

                    VideoProperties[_resourceService.GetString(ResourceName.PropertyResolution)] = $"{media.MediaInfo.VideoProperties.Width}x{media.MediaInfo.VideoProperties.Height}";
                    VideoProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps";

                    AudioProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                    break;

                case MediaPlaybackType.Music:
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTitle)] = media.MediaInfo.MusicProperties.Title;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyContributingArtists)] = media.MediaInfo.MusicProperties.Artist;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyAlbum)] = media.MediaInfo.MusicProperties.Album;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyAlbumArtist)] = media.MediaInfo.MusicProperties.AlbumArtist;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyComposers)] = string.Join("; ", media.MediaInfo.MusicProperties.Composers);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyGenre)] = string.Join("; ", media.MediaInfo.MusicProperties.Genre);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTrack)] = media.MediaInfo.MusicProperties.TrackNumber.ToString();
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyYear)] = media.MediaInfo.MusicProperties.Year > 0
                        ? media.MediaInfo.MusicProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration(media.MediaInfo.MusicProperties.Duration);

                    AudioProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                    break;
            }

            _mediaFile = media switch
            {
                FileMediaViewModel { File: { } file } => file,
                UriMediaViewModel { File: { } uriFile } => uriFile,
                _ => _mediaFile
            };

            if (_mediaFile != null)
            {
                CanNavigateToFile = true;
                FileProperties[_resourceService.GetString(ResourceName.PropertyFileType)] = _mediaFile.FileType;
                FileProperties[_resourceService.GetString(ResourceName.PropertyContentType)] = _mediaFile.ContentType;
                FileProperties[_resourceService.GetString(ResourceName.PropertySize)] = BytesToHumanReadable((long)media.MediaInfo.Size);
                FileProperties[_resourceService.GetString(ResourceName.PropertyLastModified)] = media.MediaInfo.DateModified.ToString();
            }
            else if (media is UriMediaViewModel { Uri: { } uri })
            {
                _mediaUri = uri;
                CanNavigateToFile = uri.IsFile;
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
            long absCount = (byteCount < 0 ? -byteCount : byteCount);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absCount >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (byteCount >> 50);
            }
            else if (absCount >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (byteCount >> 40);
            }
            else if (absCount >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (byteCount >> 30);
            }
            else if (absCount >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (byteCount >> 20);
            }
            else if (absCount >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (byteCount >> 10);
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
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }
    }
}
