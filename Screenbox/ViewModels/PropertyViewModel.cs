#nullable enable

using System.Collections.Generic;
using Windows.Media;
using Windows.Storage;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Converters;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class PropertyViewModel
    {
        public Dictionary<string, string> MediaProperties { get; }

        public Dictionary<string, string> VideoProperties { get; }

        public Dictionary<string, string> AudioProperties { get; }

        public Dictionary<string, string> FileProperties { get; }

        public bool CanNavigateToFile { get; private set; }

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
                    MediaProperties[Strings.Resources.PropertyTitle] = string.IsNullOrEmpty(media.VideoProperties.Title)
                        ? media.Name
                        : media.VideoProperties.Title;
                    MediaProperties[Strings.Resources.PropertySubtitle] = media.VideoProperties.Subtitle;
                    MediaProperties[Strings.Resources.PropertyYear] = media.VideoProperties.Year > 0
                        ? media.VideoProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[Strings.Resources.PropertyProducers] = string.Join("; ", media.VideoProperties.Producers);
                    MediaProperties[Strings.Resources.PropertyWriters] = string.Join("; ", media.VideoProperties.Writers);
                    MediaProperties[Strings.Resources.PropertyLength] = HumanizedDurationConverter.Convert(media.VideoProperties.Duration);

                    VideoProperties[Strings.Resources.PropertyResolution] = $"{media.VideoProperties.Width}x{media.VideoProperties.Height}";
                    VideoProperties[Strings.Resources.PropertyBitRate] = $"{media.VideoProperties.Bitrate / 1000} kbps";

                    if (media.MusicProperties != null)
                    {
                        AudioProperties[Strings.Resources.PropertyBitRate] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    }
                    break;

                case MediaPlaybackType.Music when media.MusicProperties != null:
                    MediaProperties[Strings.Resources.PropertyTitle] = media.MusicProperties.Title;
                    MediaProperties[Strings.Resources.PropertyContributingArtists] = media.MusicProperties.Artist;
                    MediaProperties[Strings.Resources.PropertyAlbum] = media.MusicProperties.Album;
                    MediaProperties[Strings.Resources.PropertyAlbumArtist] = media.MusicProperties.AlbumArtist;
                    MediaProperties[Strings.Resources.PropertyGenre] = string.Join("; ", media.MusicProperties.Genre);
                    MediaProperties[Strings.Resources.PropertyTrack] = media.MusicProperties.TrackNumber.ToString();
                    MediaProperties[Strings.Resources.PropertyYear] = media.MusicProperties.Year > 0
                        ? media.MusicProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[Strings.Resources.PropertyLength] = HumanizedDurationConverter.Convert(media.MusicProperties.Duration);

                    AudioProperties[Strings.Resources.PropertyBitRate] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    break;
            }

            if (media.Source is StorageFile file)
            {
                CanNavigateToFile = true;
                OpenFileLocationCommand.NotifyCanExecuteChanged();
                _mediaFile = file;

                FileProperties[Strings.Resources.PropertyFileType] = file.FileType;
                FileProperties[Strings.Resources.PropertyContentType] = file.ContentType;
                if (media.BasicProperties != null)
                {
                    FileProperties[Strings.Resources.PropertySize] = BytesToHumanReadable((long)media.BasicProperties.Size);
                    FileProperties[Strings.Resources.PropertyLastModified] = media.BasicProperties.DateModified.ToString();
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
