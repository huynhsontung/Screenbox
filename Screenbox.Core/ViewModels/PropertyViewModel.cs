#nullable enable

using System.Collections.Generic;
using Windows.Media;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core;
using Screenbox.Core.Services;

namespace Screenbox.ViewModels
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
        private StorageFile? _mediaFile; 

        public PropertyViewModel(IFilesService filesService)
        {
            _filesService = filesService;
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
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyTitle)] = string.IsNullOrEmpty(media.VideoProperties.Title)
                        ? media.Name
                        : media.VideoProperties.Title;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertySubtitle)] = media.VideoProperties.Subtitle;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyYear)] = media.VideoProperties.Year > 0
                        ? media.VideoProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyProducers)] = string.Join("; ", media.VideoProperties.Producers);
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyWriters)] = string.Join("; ", media.VideoProperties.Writers);
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyLength)] = Humanizer.ToDuration(media.VideoProperties.Duration);

                    VideoProperties[ResourceHelper.GetString(ResourceHelper.PropertyResolution)] = $"{media.VideoProperties.Width}x{media.VideoProperties.Height}";
                    VideoProperties[ResourceHelper.GetString(ResourceHelper.PropertyBitRate)] = $"{media.VideoProperties.Bitrate / 1000} kbps";

                    if (media.MusicProperties != null)
                    {
                        AudioProperties[ResourceHelper.GetString(ResourceHelper.PropertyBitRate)] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    }
                    break;

                case MediaPlaybackType.Music when media.MusicProperties != null:
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyTitle)] = media.MusicProperties.Title;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyContributingArtists)] = media.MusicProperties.Artist;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyAlbum)] = media.MusicProperties.Album;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyAlbumArtist)] = media.MusicProperties.AlbumArtist;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyGenre)] = string.Join("; ", media.MusicProperties.Genre);
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyTrack)] = media.MusicProperties.TrackNumber.ToString();
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyYear)] = media.MusicProperties.Year > 0
                        ? media.MusicProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[ResourceHelper.GetString(ResourceHelper.PropertyLength)] = Humanizer.ToDuration(media.MusicProperties.Duration);

                    AudioProperties[ResourceHelper.GetString(ResourceHelper.PropertyBitRate)] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    break;
            }

            if (media.Source is StorageFile file)
            {
                CanNavigateToFile = true;
                _mediaFile = file;

                FileProperties[ResourceHelper.GetString(ResourceHelper.PropertyFileType)] = file.FileType;
                FileProperties[ResourceHelper.GetString(ResourceHelper.PropertyContentType)] = file.ContentType;
                if (media.BasicProperties != null)
                {
                    FileProperties[ResourceHelper.GetString(ResourceHelper.PropertySize)] = BytesToHumanReadable((long)media.BasicProperties.Size);
                    FileProperties[ResourceHelper.GetString(ResourceHelper.PropertyLastModified)] = media.BasicProperties.DateModified.ToString();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanNavigateToFile))]
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
