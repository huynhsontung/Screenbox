#nullable enable

using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using Screenbox.Core.Enums;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

        public IPlaybackItem? Item { get; internal set; }

        public IReadOnlyList<string> Options { get; }

        public MediaPlaybackType MediaType => MediaInfo.MediaType;

        public TimeSpan Duration => MediaInfo.MusicProperties.Duration > TimeSpan.Zero
            ? MediaInfo.MusicProperties.Duration
            : MediaInfo.VideoProperties.Duration;

        public string DurationText => Duration > TimeSpan.Zero ? Humanizer.ToDuration(Duration) : string.Empty;     // Helper for binding

        public string TrackNumberText =>
            MediaInfo.MusicProperties.TrackNumber > 0 ? MediaInfo.MusicProperties.TrackNumber.ToString() : string.Empty;    // Helper for binding

        private readonly List<string> _options;

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
            Item = source.Item;
        }

        private MediaViewModel(object source, MediaInfo mediaInfo)
        {
            Source = source;
            Location = string.Empty;
            _name = string.Empty;
            _mediaInfo = mediaInfo;
            _artists = Array.Empty<ArtistViewModel>();
            _options = new List<string>();
            Options = new ReadOnlyCollection<string>(_options);
        }

        public MediaViewModel(StorageFile file)
            : this(file, new MediaInfo(FilesHelpers.GetMediaTypeForFile(file)))
        {
            Location = file.Path;
            _name = file.DisplayName;
            _altCaption = file.Name;
        }

        public MediaViewModel(Uri uri)
            : this(uri, new MediaInfo(MediaPlaybackType.Unknown))
        {
            Guard.IsTrue(uri.IsAbsoluteUri);
            Location = uri.OriginalString;
            _name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        }

        public MediaViewModel(Media media)
            : this(media, new MediaInfo(MediaPlaybackType.Unknown))
        {
            Location = media.Mrl;

            // Media is already loaded, create PlaybackItem
            Item = new VlcPlaybackItem(media, media);
        }

        partial void OnMediaInfoChanged(MediaInfo value)
        {
            UpdateCaptions();
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
            Clean();
        }

        public void Clean()
        {
            // If source is Media then there is no way to recreate. Don't clean up.
            if (Source is Media) return;
            IPlaybackItem? item = Item;
            Item = null;
            if (item is VlcPlaybackItem vlcItem)
                LibVlcService.DisposeMedia(vlcItem.Media);
        }

        public void UpdateSource(StorageFile file)
        {
            Source = file;
            Name = file.DisplayName;
            AltCaption = file.Name;
        }

        public async Task LoadDetailsAsync(IFilesService filesService)
        {
            switch (Source)
            {
                case StorageFile file:
                    MediaInfo = await filesService.GetMediaInfoAsync(file);
                    break;
                case Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri:
                    StorageFile uriFile;
                    try
                    {
                        uriFile = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                    }
                    catch (IOException)
                    {
                        return;
                    }

                    UpdateSource(uriFile);
                    MediaInfo = await filesService.GetMediaInfoAsync(uriFile);
                    break;
            }

            switch (MediaType)
            {
                case MediaPlaybackType.Unknown when Item is VlcPlaybackItem { VideoTracks.Count: 0, Media.ParsedStatus: MediaParsedStatus.Done }:
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

            if (Item is VlcPlaybackItem { Media: { IsParsed: true } media })
            {
                if (Source is not IStorageItem &&
                    media.Meta(MetadataType.Title) is { } title &&
                    !Guid.TryParse(title, out Guid _))
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
            if (Source is Uri { IsFile: true, IsLoopback: true, IsAbsoluteUri: true } uri)
            {
                try
                {
                    StorageFile uriFile = await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                    UpdateSource(uriFile);
                }
                catch (IOException)
                {
                    return;
                }
            }

            if (Source is StorageFile file)
            {
                StorageItemThumbnail? source = await filesService.GetThumbnailAsync(file);
                if (source == null) return;
                ThumbnailSource = source;
                BitmapImage image = new();

                try
                {
                    await image.SetSourceAsync(ThumbnailSource);
                }
                catch (Exception)
                {
                    // WinRT component not found exception???
                    return;
                }

                Thumbnail = image;
            }
            else if (Item is VlcPlaybackItem { Media: { } media } &&
                     media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
                     Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri artworkUri))
            {
                Thumbnail = new BitmapImage(artworkUri);
            }
        }

        public void UpdateAlbum(AlbumViewModelFactory factory)
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music) return;
            MusicInfo musicProperties = MediaInfo.MusicProperties;
            if (Album != null)
            {
                if (factory.GetAlbumFromName(musicProperties.Album, musicProperties.AlbumArtist) == Album)
                    return;

                factory.Remove(this);
            }

            Album = factory.AddSongToAlbum(this, musicProperties.Album, musicProperties.AlbumArtist, musicProperties.Year);
        }

        public void UpdateArtists(ArtistViewModelFactory factory)
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music) return;
            if (Artists.Length > 0)
            {
                ArtistViewModel[] artists = factory.ParseArtists(MediaInfo.MusicProperties.Artist);
                if (artists.SequenceEqual(Artists)) return;
                factory.Remove(this);
            }

            Artists = factory.ParseAddArtists(MediaInfo.MusicProperties.Artist, this);
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

            if (Item is VlcPlaybackItem { Media: { IsParsed: true } media })
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