#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Playback;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class MediaViewModel : ObservableObject
    {
        public string Location { get; }

        public object Source { get; }

        public string Glyph { get; }

        public PlaybackItem Item => _item ??= Source is StorageFile file
            ? PlaybackItem.GetFromStorageFile(file)
            : PlaybackItem.GetFromUri((Uri)Source);

        private PlaybackItem? _item;

        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private TimeSpan? _duration;
        [ObservableProperty] private BitmapImage? _thumbnail;
        [ObservableProperty] private VideoProperties? _videoProperties;
        [ObservableProperty] private MusicProperties? _musicProperties;
        [ObservableProperty] private string? _genre;

        private readonly StorageItemViewModel? _linkedFile;

        public MediaViewModel(MediaViewModel source)
        {
            _linkedFile = source._linkedFile;
            _item = source._item;
            _name = source._name;
            Thumbnail = source.Thumbnail;
            Location = source.Location;
            Duration = source.Duration;
            Source = source.Source;
            Glyph = source.Glyph;
        }

        public MediaViewModel(StorageItemViewModel linkedVm, StorageFile file) : this(file)
        {
            _linkedFile = linkedVm;
        }

        public MediaViewModel(Uri uri)
        {
            Source = uri;
            _name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            Location = uri.ToString();
            Glyph = "\ue774"; // Globe icon
        }

        public MediaViewModel(StorageFile file)
        {
            Source = file;
            _name = file.Name;
            Location = file.Path;
            Glyph = StorageItemViewModel.GetGlyph(file);
        }

        public async Task LoadDetailsAsync()
        {
            if (Source is not StorageFile file) return;
            if (VideoProperties != null || MusicProperties != null) return;
            if (file.ContentType.StartsWith("video"))
            {
                VideoProperties = await file.Properties.GetVideoPropertiesAsync();
                if (VideoProperties != null)
                {
                    if (VideoProperties.Duration != TimeSpan.Zero)
                    {
                        Duration = VideoProperties.Duration;
                    }

                    if (!string.IsNullOrEmpty(VideoProperties.Title))
                    {
                        Name = VideoProperties.Title;
                    }
                }
            }
            else if (file.ContentType.StartsWith("audio"))
            {
                MusicProperties = await file.Properties.GetMusicPropertiesAsync();
                if (MusicProperties != null)
                {
                    if (MusicProperties.Duration != TimeSpan.Zero)
                    {
                        Duration = MusicProperties.Duration;
                    }

                    if (!string.IsNullOrEmpty(MusicProperties.Title))
                    {
                        Name = MusicProperties.Title;
                    }

                    if (MusicProperties.Genre.Count > 0)
                    {
                        Genre = MusicProperties.Genre[0];
                    }
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

        partial void OnIsPlayingChanged(bool value)
        {
            if (_linkedFile != null)
            {
                _linkedFile.IsPlaying = value;
            }
        }
    }
}