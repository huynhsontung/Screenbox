#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels
{
    public partial class MediaViewModel : ObservableRecipient
    {
        public string Location { get; }

        public object Source { get; private set; }

        public bool IsFromLibrary { get; set; }

        public StorageItemThumbnail? ThumbnailSource { get; set; }

        public ArtistViewModel? MainArtist => Artists.FirstOrDefault();

        public PlaybackItem? Item
        {
            get => _item ?? GetPlaybackItem();
            internal set => _loaded = (_item = value) != null;  // Only set on init. Don't need to worry about clean up in this case.
        }

        public IReadOnlyList<string> Options { get; }

        public MediaPlaybackType MediaType => MediaInfo.MediaType;

        public TimeSpan Duration => MediaInfo.MusicProperties.Duration > TimeSpan.Zero
            ? MediaInfo.MusicProperties.Duration
            : MediaInfo.VideoProperties.Duration;

        public string DurationText => Duration > TimeSpan.Zero ? Humanizer.ToDuration(Duration) : string.Empty;     // Helper for binding

        public string TrackNumberText =>
            MediaInfo.MusicProperties.TrackNumber > 0 ? MediaInfo.MusicProperties.TrackNumber.ToString() : string.Empty;    // Helper for binding

        private readonly IMediaService _mediaService;
        private readonly List<string> _options;
        private readonly AlbumViewModelFactory _albumFactory;
        private readonly ArtistViewModelFactory _artistFactory;
        private PlaybackItem? _item;
        private bool _loaded;

        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _isMediaActive;
        [ObservableProperty] private BitmapImage? _thumbnail;
        [ObservableProperty] private AlbumViewModel? _album;
        [ObservableProperty] private string? _caption;  // For list item subtitle
        [ObservableProperty] private string? _altCaption;   // For player page subtitle

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DurationText))]
        [NotifyPropertyChangedFor(nameof(TrackNumberText))]
        private MediaInfo _mediaInfo;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainArtist))]
        private ArtistViewModel[] _artists;

        [ObservableProperty]
        private bool? _isPlaying;

        public MediaViewModel(MediaViewModel source)
        {
            _mediaService = source._mediaService;
            _albumFactory = source._albumFactory;
            _artistFactory = source._artistFactory;
            _item = source._item;
            _name = source._name;
            _thumbnail = source._thumbnail;
            _mediaInfo = source._mediaInfo;
            _artists = source._artists;
            _album = source._album;
            _caption = source._caption;
            _altCaption = source._altCaption;
            _options = new List<string>(source.Options);
            Options = new ReadOnlyCollection<string>(_options);
            Location = source.Location;
            Source = source.Source;
        }

        private MediaViewModel(object source, IMediaService mediaService, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory)
        {
            _mediaService = mediaService;
            _albumFactory = albumFactory;
            _artistFactory = artistFactory;
            Source = source;
            Location = string.Empty;
            _name = string.Empty;
            _mediaInfo = new MediaInfo();
            _artists = Array.Empty<ArtistViewModel>();
            _options = new List<string>();
            Options = new ReadOnlyCollection<string>(_options);
        }

        public MediaViewModel(IMediaService mediaService, AlbumViewModelFactory albumFactory,
            ArtistViewModelFactory artistFactory, StorageFile file) : this(file, mediaService, albumFactory, artistFactory)
        {
            Location = file.Path;
            _name = file.DisplayName;
            _altCaption = file.Name;
        }

        public MediaViewModel(IMediaService mediaService, AlbumViewModelFactory albumFactory,
            ArtistViewModelFactory artistFactory, Uri uri) : this(uri, mediaService, albumFactory, artistFactory)
        {
            Location = uri.OriginalString;
            _name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        }

        public MediaViewModel(IMediaService mediaService, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, Media media)
            : this(media, mediaService, albumFactory, artistFactory)
        {
            Location = media.Mrl;

            // Media is already loaded, create PlaybackItem
            _loaded = true;
            _item = new PlaybackItem(media, media);
        }

        partial void OnMediaInfoChanged(MediaInfo value)
        {
            UpdateCaptions();
            UpdateArtists();
            UpdateAlbum();
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

        public async Task LoadDetailsAsync(IFilesService filesService)
        {
            switch (Source)
            {
                case StorageFile file:
                    MediaInfo = await filesService.GetMediaInfoAsync(file);
                    break;
                case Uri { IsFile: true } uri:
                    StorageFile uriFile = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                    Source = uriFile;
                    Name = uriFile.DisplayName;
                    AltCaption = uriFile.Name;
                    MediaInfo = await filesService.GetMediaInfoAsync(uriFile);
                    break;
            }

            switch (MediaType)
            {
                case MediaPlaybackType.Unknown when _item is { VideoTracks.Count: 0 }:
                    // Update media type when it was previously set Unknown. Usually when source is a URI.
                    // We don't want to init PlaybackItem just for this.
                    MediaInfo.MediaType = MediaPlaybackType.Music;
                    break;
                case MediaPlaybackType.Music when !string.IsNullOrEmpty(MediaInfo.MusicProperties.Title):
                    Name = MediaInfo.MusicProperties.Title;
                    break;
                case MediaPlaybackType.Video when !string.IsNullOrEmpty(MediaInfo.VideoProperties.Title):
                    Name = MediaInfo.VideoProperties.Title;
                    break;
            }

            if (_item?.Media is { IsParsed: true } media)
            {
                if (media.Meta(MetadataType.Title) is { } title && !title.StartsWith('{'))
                {
                    Name = title;
                }

                VideoInfo videoProperties = MediaInfo.VideoProperties;
                videoProperties.ShowName = media.Meta(MetadataType.ShowName) ?? videoProperties.ShowName;
                videoProperties.Season = media.Meta(MetadataType.Season) ?? videoProperties.Season;
                videoProperties.Episode = media.Meta(MetadataType.Episode) ?? videoProperties.Episode;
            }
        }

        public async Task LoadThumbnailAsync(IFilesService filesService)
        {
            if (Thumbnail != null) return;
            if (Source is StorageFile file)
            {
                StorageItemThumbnail? source = await filesService.GetThumbnailAsync(file);
                if (source == null) return;
                ThumbnailSource = source;
                BitmapImage image = new();
                Thumbnail = image;
                await image.SetSourceAsync(ThumbnailSource);
            }
            else if (_item?.Media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
                     Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri uri))
            {
                Thumbnail = new BitmapImage(uri);
            }
        }

        public void UpdateAlbum()
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music || Album != null) return;
            MusicInfo musicProperties = MediaInfo.MusicProperties;
            Album = _albumFactory.AddSongToAlbum(this, musicProperties.Album, musicProperties.AlbumArtist, musicProperties.Year);
        }

        public void UpdateArtists()
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music || Artists.Length != 0) return;
            Artists = _artistFactory.ParseArtists(MediaInfo.MusicProperties.Artist, this);
        }

        private void UpdateCaptions()
        {
            if (Duration > TimeSpan.Zero)
            {
                Caption = Humanizer.ToDuration(Duration);
            }

            MusicInfo musicProperties = MediaInfo.MusicProperties;
            if (!string.IsNullOrEmpty(musicProperties.Artist))
            {
                Caption = musicProperties.Artist;
                AltCaption = string.IsNullOrEmpty(musicProperties.Album)
                    ? musicProperties.Artist
                    : $"{musicProperties.Artist} – {musicProperties.Album}";
            }
            else if (!string.IsNullOrEmpty(musicProperties.Album))
            {
                AltCaption = musicProperties.Album;
            }

            if (_item?.Media is { IsParsed: true } media)
            {
                string artist = media.Meta(MetadataType.Artist) ?? string.Empty;
                if (!string.IsNullOrEmpty(artist))
                {
                    Caption = artist;
                }

                if (media.Meta(MetadataType.Album) is { } album && !string.IsNullOrEmpty(album))
                {
                    AltCaption = string.IsNullOrEmpty(artist) ? album : $"{artist} – {album}";
                }
            }
        }
    }
}