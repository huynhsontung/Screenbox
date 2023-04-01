#nullable enable

using System.Collections.Generic;
using Windows.Media;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class PropertyViewModel : ObservableObject
    {
        public Dictionary<string, string> MediaProperties { get; }

        public Dictionary<string, string> VideoProperties { get; }

        public Dictionary<string, string> AudioProperties { get; }

        public Dictionary<string, string> FileProperties { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PropertyViewModel.OpenFileLocationCommand))]
        private bool _canNavigateToFile;

        private readonly IFilesService _filesService;
        private readonly IResourceService _resourceService;
        private StorageFile? _mediaFile; 

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
                case MediaPlaybackType.Video when media.VideoProperties != null:
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTitle)] = string.IsNullOrEmpty(media.VideoProperties.Title)
                        ? media.Name
                        : media.VideoProperties.Title;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertySubtitle)] = media.VideoProperties.Subtitle;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyYear)] = media.VideoProperties.Year > 0
                        ? media.VideoProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyProducers)] = string.Join("; ", media.VideoProperties.Producers);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyWriters)] = string.Join("; ", media.VideoProperties.Writers);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration(media.VideoProperties.Duration);

                    VideoProperties[_resourceService.GetString(ResourceName.PropertyResolution)] = $"{media.VideoProperties.Width}x{media.VideoProperties.Height}";
                    VideoProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.VideoProperties.Bitrate / 1000} kbps";

                    if (media.MusicProperties != null)
                    {
                        AudioProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    }
                    break;

                case MediaPlaybackType.Music when media.MusicProperties != null:
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTitle)] = media.MusicProperties.Title;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyContributingArtists)] = media.MusicProperties.Artist;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyAlbum)] = media.MusicProperties.Album;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyAlbumArtist)] = media.MusicProperties.AlbumArtist;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyGenre)] = string.Join("; ", media.MusicProperties.Genre);
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyTrack)] = media.MusicProperties.TrackNumber.ToString();
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyYear)] = media.MusicProperties.Year > 0
                        ? media.MusicProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[_resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration(media.MusicProperties.Duration);

                    AudioProperties[_resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    break;
            }

            if (media.Source is StorageFile file)
            {
                CanNavigateToFile = true;
                _mediaFile = file;

                FileProperties[_resourceService.GetString(ResourceName.PropertyFileType)] = file.FileType;
                FileProperties[_resourceService.GetString(ResourceName.PropertyContentType)] = file.ContentType;
                if (media.BasicProperties != null)
                {
                    FileProperties[_resourceService.GetString(ResourceName.PropertySize)] = BytesToHumanReadable((long)media.BasicProperties.Size);
                    FileProperties[_resourceService.GetString(ResourceName.PropertyLastModified)] = media.BasicProperties.DateModified.ToString();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(PropertyViewModel.CanNavigateToFile))]
        private void OpenFileLocation()
        {
            if (_mediaFile == null) return;
            _filesService.OpenFileLocationAsync(_mediaFile);
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
