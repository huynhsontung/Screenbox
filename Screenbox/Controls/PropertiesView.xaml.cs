#nullable enable

using System.Collections.Generic;
using Windows.Media;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Screenbox.Converters;
using Screenbox.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls
{
    public sealed partial class PropertiesView : UserControl
    {
        public static readonly DependencyProperty MediaProperty = DependencyProperty.Register(
            "Media",
            typeof(MediaViewModel),
            typeof(PropertiesView),
            new PropertyMetadata(null, OnMediaChanged));

        internal MediaViewModel? Media
        {
            get => (MediaViewModel?)GetValue(MediaProperty);
            set => SetValue(MediaProperty, value);
        }

        private readonly Dictionary<string, string> _mediaProperties;
        private readonly Dictionary<string, string> _videoProperties;
        private readonly Dictionary<string, string> _audioProperties;
        private readonly Dictionary<string, string> _fileProperties;
        private bool _canNavigateToFile;

        public PropertiesView()
        {
            this.InitializeComponent();
            _mediaProperties = new Dictionary<string, string>();
            _videoProperties = new Dictionary<string, string>();
            _audioProperties = new Dictionary<string, string>();
            _fileProperties = new Dictionary<string, string>();
        }

        private static void OnMediaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertiesView view = (PropertiesView)d;
            MediaViewModel? media = (MediaViewModel?)e.NewValue;
            if (media == null) return;

            view._canNavigateToFile = media.Source is StorageFile;
            view.UpdateProperties(media);
        }

        private void UpdateProperties(MediaViewModel media)
        {
            switch (media.MediaType)
            {
                case MediaPlaybackType.Video when media.VideoProperties != null:
                    _mediaProperties["Title"] = string.IsNullOrEmpty(media.VideoProperties.Title)
                        ? media.Name
                        : media.VideoProperties.Title;
                    _mediaProperties["Subtitle"] = media.VideoProperties.Subtitle;
                    _mediaProperties["Year"] = media.VideoProperties.Year > 0
                        ? media.VideoProperties.Year.ToString()
                        : string.Empty;
                    _mediaProperties["Producers"] = string.Join("; ", media.VideoProperties.Producers);
                    _mediaProperties["Writers"] = string.Join("; ", media.VideoProperties.Writers);
                    _mediaProperties["Length"] = HumanizedDurationConverter.Convert(media.VideoProperties.Duration);

                    _videoProperties["Resolution"] = $"{media.VideoProperties.Width}x{media.VideoProperties.Height}";
                    _videoProperties["Bit rate"] = $"{media.VideoProperties.Bitrate / 1000} kbps";

                    if (media.MusicProperties != null)
                    {
                        _audioProperties["Bit rate"] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    }
                    break;

                case MediaPlaybackType.Music when media.MusicProperties != null:
                    _mediaProperties["Title"] = media.MusicProperties.Title;
                    _mediaProperties["Contributing artists"] = media.MusicProperties.Artist;
                    _mediaProperties["Album"] = media.MusicProperties.Album;
                    _mediaProperties["Album artists"] = media.MusicProperties.AlbumArtist;
                    _mediaProperties["Genre"] = string.Join("; ", media.MusicProperties.Genre);
                    _mediaProperties["Track"] = media.MusicProperties.TrackNumber.ToString();
                    _mediaProperties["Year"] = media.MusicProperties.Year > 0
                        ? media.MusicProperties.Year.ToString()
                        : string.Empty;
                    _mediaProperties["Length"] = HumanizedDurationConverter.Convert(media.MusicProperties.Duration);

                    _audioProperties["Bit rate"] = $"{media.MusicProperties.Bitrate / 1000} kbps";
                    break;

            }

            if (media.Source is StorageFile file)
            {
                _fileProperties["File type"] = file.FileType;
                _fileProperties["Content type"] = file.ContentType;
                if (media.BasicProperties != null)
                {
                    _fileProperties["Size"] = BytesToHumanReadable((long)media.BasicProperties.Size);
                    _fileProperties["Last modified"] = media.BasicProperties.DateModified.ToString();
                    _fileProperties["Item date"] = media.BasicProperties.ItemDate.ToString();
                }
            }
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
