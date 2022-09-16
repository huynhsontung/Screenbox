#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Converters;
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
        private Task _loadTask;
        private Task _loadThumbnailTask;

        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private TimeSpan? _duration;
        [ObservableProperty] private BitmapImage? _thumbnail;
        [ObservableProperty] private BasicProperties? _basicProperties;
        [ObservableProperty] private VideoProperties? _videoProperties;
        [ObservableProperty] private MusicProperties? _musicProperties;
        [ObservableProperty] private string? _genre;
        [ObservableProperty] private ArtistViewModel[]? _artists;
        [ObservableProperty] private AlbumViewModel? _album;
        [ObservableProperty] private MediaPlaybackType _mediaType;

        private MediaViewModel(MediaViewModel source)
        {
            _item = source._item;
            _name = source._name;
            _loadTask = source._loadTask;
            _loadThumbnailTask = source._loadThumbnailTask;
            Thumbnail = source.Thumbnail;
            Location = source.Location;
            Duration = source.Duration;
            Source = source.Source;
            Glyph = source.Glyph;
        }

        public MediaViewModel(Uri uri)
        {
            Source = uri;
            _name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            _mediaType = MediaPlaybackType.Unknown;
            _loadTask = Task.CompletedTask;
            _loadThumbnailTask = Task.CompletedTask;
            Location = uri.ToString();
            Glyph = "\ue774"; // Globe icon
        }

        public MediaViewModel(StorageFile file)
        {
            Source = file;
            _name = file.Name;
            _loadTask = Task.CompletedTask;
            _loadThumbnailTask = Task.CompletedTask;
            _mediaType = GetMediaTypeForFile(file);
            Location = file.Path;
            Glyph = StorageItemGlyphConverter.Convert(file);
        }

        public MediaViewModel Clone()
        {
            return new MediaViewModel(this);
        }

        public void Clean()
        {
            Thumbnail = null;
        }

        public async Task LoadDetailsAndThumbnailAsync()
        {
            await LoadDetailsAsync();
            await LoadThumbnailAsync();
        }

        public async Task LoadTitleAsync()
        {
            if (Source is not StorageFile file) return;
            string[] propertyKeys = { SystemProperties.Title };
            IDictionary<string, object> properties = await file.Properties.RetrievePropertiesAsync(propertyKeys);
            if (properties[SystemProperties.Title] is string name && !string.IsNullOrEmpty(name))
            {
                Name = name;
            }
        }

        public Task LoadDetailsAsync()
        {
            if (!_loadTask.IsCompleted) return _loadTask;
            _loadTask = LoadDetailsInternalAsync();
            return _loadTask;
        }

        private async Task LoadDetailsInternalAsync()
        {
            if (Source is not StorageFile file) return;
            string[] additionalPropertyKeys =
            {
                SystemProperties.Title,
                SystemProperties.Music.Artist,
                SystemProperties.Media.Duration
            };

            IDictionary<string, object> additionalProperties = await file.Properties.RetrievePropertiesAsync(additionalPropertyKeys);
            if (additionalProperties[SystemProperties.Title] is string name && !string.IsNullOrEmpty(name))
            {
                Name = name;
            }

            if (additionalProperties[SystemProperties.Media.Duration] is ulong ticks and > 0)
            {
                Duration = TimeSpan.FromTicks((long)ticks);
            }

            BasicProperties ??= await file.GetBasicPropertiesAsync();

            switch (MediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties ??= await file.Properties.GetVideoPropertiesAsync();
                    break;
                case MediaPlaybackType.Music:
                    MusicProperties ??= await file.Properties.GetMusicPropertiesAsync();
                    if (MusicProperties != null)
                    {
                        Genre ??= MusicProperties.Genre.Count > 0 ? MusicProperties.Genre[0] : Strings.Resources.UnknownGenre;
                        Album ??= AlbumViewModel.GetAlbumForSong(this, MusicProperties.Album, MusicProperties.AlbumArtist);

                        if (Artists == null)
                        {
                            if (additionalProperties[SystemProperties.Music.Artist] is not string[] contributingArtists ||
                                contributingArtists.Length == 0)
                            {
                                Artists = new[] { ArtistViewModel.GetArtistForSong(this, string.Empty) };
                            }
                            else
                            {
                                Artists = contributingArtists
                                    .Select(artist => ArtistViewModel.GetArtistForSong(this, artist))
                                    .ToArray();
                            }
                        }
                    }

                    break;
            }
        }

        public Task LoadThumbnailAsync()
        {
            if (!_loadThumbnailTask.IsCompleted) return _loadThumbnailTask;
            _loadThumbnailTask = LoadThumbnailInternalAsync();
            return _loadThumbnailTask;
        }

        public async Task LoadThumbnailInternalAsync()
        {
            if (Thumbnail == null && Source is StorageFile file)
            {
                IFilesService filesService = App.Services.GetRequiredService<IFilesService>();
                Thumbnail = await filesService.GetThumbnailAsync(file);
            }
        }

        private static MediaPlaybackType GetMediaTypeForFile(IStorageFile file)
        {
            if (file.ContentType.StartsWith("video")) return MediaPlaybackType.Video;
            if (file.ContentType.StartsWith("audio")) return MediaPlaybackType.Music;
            if (file.ContentType.StartsWith("image")) return MediaPlaybackType.Image;
            return MediaPlaybackType.Unknown;
        }
    }
}