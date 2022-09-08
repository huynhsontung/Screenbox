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
        private Task _loadingTask;

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
            _loadingTask = source._loadingTask;
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
            _loadingTask = Task.CompletedTask;
            Location = uri.ToString();
            Glyph = "\ue774"; // Globe icon
        }

        public MediaViewModel(StorageFile file)
        {
            Source = file;
            _name = file.Name;
            _loadingTask = Task.CompletedTask;
            Location = file.Path;
            Glyph = StorageItemGlyphConverter.Convert(file);
        }

        public MediaViewModel Clone()
        {
            return new MediaViewModel(this);
        }

        public Task LoadDetailsAsync()
        {
            if (!_loadingTask.IsCompleted) return _loadingTask;
            _loadingTask = LoadDetailsInternalAsync();
            return _loadingTask;
        }

        private async Task LoadDetailsInternalAsync()
        {
            if (Source is not StorageFile file) return;
            BasicProperties ??= await file.GetBasicPropertiesAsync();
            VideoProperties ??= await file.Properties.GetVideoPropertiesAsync();
            MusicProperties ??= await file.Properties.GetMusicPropertiesAsync();
            if (file.ContentType.StartsWith("video"))
            {
                MediaType = MediaPlaybackType.Video;
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
                MediaType = MediaPlaybackType.Music;
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

                    Genre ??= MusicProperties.Genre.Count > 0 ? MusicProperties.Genre[0] : Strings.Resources.UnknownGenre;
                    Album ??= AlbumViewModel.GetAlbumForSong(this, MusicProperties.Album, MusicProperties.AlbumArtist);

                    if (Artists == null)
                    {
                        string[] contributingArtistsKey = { "System.Music.Artist" };
                        IDictionary<string, object> contributingArtistsProperty =
                            await MusicProperties.RetrievePropertiesAsync(contributingArtistsKey);
                        if (contributingArtistsProperty["System.Music.Artist"] is not string[] contributingArtists ||
                            contributingArtists.Length == 0)
                        {
                            Artists = new[] { ArtistViewModel.GetArtistForSong(this, string.Empty) };
                        }
                        else
                        {
                            Artists = contributingArtists.Select(artist => ArtistViewModel.GetArtistForSong(this, artist))
                                .ToArray();
                        }
                    }
                }
            }

            await LoadThumbnailAsync();
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