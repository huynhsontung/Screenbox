#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class MediaViewModel : ObservableRecipient
    {
        public string Location { get; }

        public object Source { get; }

        public StorageItemThumbnail? ThumbnailSource { get; set; }

        public ArtistViewModel? MainArtist => Artists.FirstOrDefault();

        public PlaybackItem? Item => _item ?? GetPlaybackItem();

        public IReadOnlyList<string> Options { get; }

        public string TrackNumberText => TrackNumber > 0 ? TrackNumber.ToString() : string.Empty;    // Helper for binding

        private readonly IFilesService _filesService;
        private readonly IMediaService _mediaService;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private readonly List<string> _options;
        private PlaybackItem? _item;
        private Task _loadTask;
        private Task _loadThumbnailTask;
        private bool _loaded;

        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _isMediaActive;
        [ObservableProperty] private TimeSpan? _duration;
        [ObservableProperty] private BitmapImage? _thumbnail;
        [ObservableProperty] private BasicProperties? _basicProperties;
        [ObservableProperty] private VideoProperties? _videoProperties;
        [ObservableProperty] private MusicProperties? _musicProperties;
        [ObservableProperty] private string? _genre;
        [ObservableProperty] private AlbumViewModel? _album;
        [ObservableProperty] private MediaPlaybackType _mediaType;
        [ObservableProperty] private string? _caption;  // For list item subtitle
        [ObservableProperty] private string? _altCaption;   // For player page subtitle
        [ObservableProperty] private uint _year;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainArtist))]
        private ArtistViewModel[] _artists;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TrackNumberText))]
        private uint _trackNumber;

        [ObservableProperty]
        private bool? _isPlaying;

        private MediaViewModel(MediaViewModel source)
        {
            _filesService = source._filesService;
            _mediaService = source._mediaService;
            _albumFactory = source._albumFactory;
            _artistFactory = source._artistFactory;
            _item = source._item;
            _name = source._name;
            _loadTask = source._loadTask;
            _loadThumbnailTask = source._loadThumbnailTask;
            _duration = source._duration;
            _thumbnail = source._thumbnail;
            _mediaType = source._mediaType;
            _basicProperties = source._basicProperties;
            _videoProperties = source._videoProperties;
            _musicProperties = source._musicProperties;
            _genre = source._genre;
            _artists = source._artists;
            _album = source._album;
            _caption = source._caption;
            _altCaption = source._altCaption;
            _options = new List<string>(source.Options);
            Options = new ReadOnlyCollection<string>(_options);
            Location = source.Location;
            Source = source.Source;
        }

        private MediaViewModel(object source, IFilesService filesService, IMediaService mediaService,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _filesService = filesService;
            _mediaService = mediaService;
            _artistFactory = artistFactory;
            _albumFactory = albumFactory;
            Source = source;

            Location = string.Empty;
            _name = string.Empty;
            _mediaType = MediaPlaybackType.Unknown;
            _loadTask = Task.CompletedTask;
            _loadThumbnailTask = Task.CompletedTask;
            _artists = Array.Empty<ArtistViewModel>();
            _options = new List<string>();
            Options = new ReadOnlyCollection<string>(_options);
        }

        public MediaViewModel(IFilesService filesService, IMediaService mediaService,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, IStorageFile file) :
            this(file, filesService, mediaService, albumFactory, artistFactory)
        {
            _name = file.Name;
            _mediaType = GetMediaTypeForFile(file);
            Location = file.Path;
        }

        public MediaViewModel(IFilesService filesService, IMediaService mediaService,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, Uri uri) :
            this(uri, filesService, mediaService, albumFactory, artistFactory)
        {
            _name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
            Location = uri.ToString();
        }

        public MediaViewModel(IFilesService filesService, IMediaService mediaService,
            AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, Media media) :
            this(media, filesService, mediaService, albumFactory, artistFactory)
        {
            _name = media.Meta(MetadataType.Title) ?? string.Empty;
            Location = media.Mrl;

            // Media is already loaded, create PlaybackItem
            _loaded = true;
            if (Uri.TryCreate(Location, UriKind.Absolute, out Uri uri))
            {
                // Prefer URI source for easier clean up
                Source = uri;
                _item = new PlaybackItem(uri, media);
            }
            else
            {
                // PlaybackItem will not be clean up in this case
                _item = new PlaybackItem(media, media);
            }
        }

        public MediaViewModel Clone()
        {
            return new MediaViewModel(this);
        }

        private PlaybackItem? GetPlaybackItem()
        {
            if (_loaded) return _item;
            _loaded = true;
            try
            {
                Media media = _mediaService.CreateMedia(Source, _options.ToArray());
                _item = new PlaybackItem(Source, media);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Coding error. Rethrow.
                throw;
            }
            catch (Exception e)
            {
                Messenger.Send(new MediaLoadFailedNotificationMessage(e.Message, Location));
            }

            return _item;
        }

        public void SetOptions(string options)
        {
            string[] opts = options.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(o => o.StartsWith(":") && o.Length > 1).ToArray();

            // Check if new options and existing options are the same
            if (opts.Length == _options.Count)
            {
                bool same = !opts.Where((o, i) => o != _options[i]).Any();
                if (same) return;
            }

            _options.Clear();
            _options.AddRange(opts);

            if (_item == null) return;
            Clean();
            GetPlaybackItem();
        }

        public void Clean()
        {
            // If source is Media then there is no way to recreate. Don't clean up.
            if (Source is Media) return;
            _loaded = false;
            PlaybackItem? item = _item;
            _item = null;
            if (item == null) return;
            _mediaService.DisposeMedia(item.Media);
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
            if (Source is not StorageFile { IsAvailable: true } file) return;
            string[] additionalPropertyKeys =
            {
                SystemProperties.Title,
                SystemProperties.Music.Artist,
                SystemProperties.Media.Duration
            };

            try
            {
                IDictionary<string, object> additionalProperties = await file.Properties.RetrievePropertiesAsync(additionalPropertyKeys);
                if (additionalProperties[SystemProperties.Title] is string name && !string.IsNullOrEmpty(name))
                {
                    Name = name;
                    if (MediaType == MediaPlaybackType.Video && name != file.Name)
                    {
                        AltCaption = file.Name;
                    }
                }

                if (additionalProperties[SystemProperties.Media.Duration] is ulong ticks and > 0)
                {
                    TimeSpan duration = TimeSpan.FromTicks((long)ticks);
                    Duration = duration;
                    Caption = Humanizer.ToDuration(duration);
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
                            TrackNumber = MusicProperties.TrackNumber;
                            Year = MusicProperties.Year;
                            Genre ??= MusicProperties.Genre.Count > 0 ? MusicProperties.Genre[0] : null;
                            Album ??= _albumFactory.AddSongToAlbum(this, MusicProperties.Album, MusicProperties.AlbumArtist, Year);

                            if (Artists.Length == 0)
                            {
                                string[] contributingArtists =
                                    additionalProperties[SystemProperties.Music.Artist] as string[] ??
                                    Array.Empty<string>();
                                Artists = _artistFactory.ParseArtists(contributingArtists, this);
                            }

                            if (string.IsNullOrEmpty(MusicProperties.Artist))
                            {
                                AltCaption = MusicProperties.Album;
                            }
                            else
                            {
                                Caption = MusicProperties.Artist;
                                AltCaption = string.IsNullOrEmpty(MusicProperties.Album)
                                    ? MusicProperties.Artist
                                    : $"{MusicProperties.Artist} – {MusicProperties.Album}";
                            }
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                // System.Exception: The RPC server is unavailable.
                if (e.HResult != unchecked((int)0x800706BA))
                    LogService.Log(e);
            }
        }

        public Task LoadThumbnailAsync()
        {
            if (!_loadThumbnailTask.IsCompleted) return _loadThumbnailTask;
            _loadThumbnailTask = LoadThumbnailInternalAsync();
            return _loadThumbnailTask;
        }

        private async Task LoadThumbnailInternalAsync()
        {
            if (Thumbnail == null && Source is StorageFile file)
            {
                StorageItemThumbnail? source = ThumbnailSource = await _filesService.GetThumbnailAsync(file);
                if (source == null) return;
                BitmapImage image = new();
                await image.SetSourceAsync(ThumbnailSource);
                Thumbnail = image;
            }
        }

        private static MediaPlaybackType GetMediaTypeForFile(IStorageFile file)
        {
            if (file.IsSupportedVideo()) return MediaPlaybackType.Video;
            if (file.IsSupportedAudio()) return MediaPlaybackType.Music;
            if (file.ContentType.StartsWith("image")) return MediaPlaybackType.Image;
            // TODO: Support playlist type
            return MediaPlaybackType.Unknown;
        }
    }
}