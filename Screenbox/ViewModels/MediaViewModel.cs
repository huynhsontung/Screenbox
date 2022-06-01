#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MediaViewModel : ObservableObject
    {
        public string Name { get; }

        public string Location { get; }

        public object Source { get; }

        public string Glyph { get; }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                SetProperty(ref _isPlaying, value);
                if (_linkedFile != null)
                {
                    _linkedFile.IsPlaying = value;
                }
            }
        }

        private bool _isPlaying;

        [ObservableProperty] private TimeSpan? _duration;
        [ObservableProperty] private BitmapImage? _thumbnail;
        [ObservableProperty] private VideoProperties? _videoProperties;
        [ObservableProperty] private MusicProperties? _musicProperties;

        private readonly StorageItemViewModel? _linkedFile;

        public MediaViewModel(MediaViewModel source)
        {
            _linkedFile = source._linkedFile;
            Name = source.Name;
            Thumbnail = source.Thumbnail;
            Location = source.Location;
            Duration = source.Duration;
            Source = source.Source;
            Glyph = source.Glyph;
        }

        public MediaViewModel(StorageItemViewModel file)
        {
            _linkedFile = file;
            Name = file.Name;
            Location = file.Path;
            Source = file.StorageItem;
            Glyph = file.Glyph;
        }

        public MediaViewModel(Uri uri)
        {
            Source = uri;
            Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            Location = uri.ToString();
            Glyph = "\ue774";   // Globe icon
        }

        public MediaViewModel(IStorageFile file)
        {
            Source = file;
            Name = file.Name;
            Location = file.Path;
            Glyph = StorageItemViewModel.GetGlyph(file);
        }

        public async Task LoadDetailsAsync()
        {
            if (Source is not StorageFile file) return;
            if (file.ContentType.StartsWith("video"))
            {
                VideoProperties = await file.Properties.GetVideoPropertiesAsync();
                if (VideoProperties != null && VideoProperties.Duration != TimeSpan.Zero)
                {
                    Duration = VideoProperties.Duration;
                }
            }
            else if (file.ContentType.StartsWith("audio"))
            {
                MusicProperties = await file.Properties.GetMusicPropertiesAsync();
                if (MusicProperties != null && MusicProperties.Duration != TimeSpan.Zero)
                {
                    Duration = MusicProperties.Duration;
                }
            }
        }

        public async Task LoadThumbnailAsync()
        {
            if (Thumbnail == null && Source is StorageFile file)
            {
                IFilesService filesService = App.Services.GetRequiredService<IFilesService>();
                Thumbnail = await filesService.GetThumbnailAsync(file);
            }
        }
    }
}
