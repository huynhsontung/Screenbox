#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels
{
    public partial class MediaViewModel : ObservableRecipient
    {
        public string Location { get; protected set; }

        public object Source { get; }

        public StorageItemThumbnail? ThumbnailSource { get; set; }

        public ArtistViewModel? MainArtist => Artists.FirstOrDefault();

        public PlaybackItem? Item
        {
            get => _item ?? GetPlaybackItem();
            internal set => _loaded = (_item = value) != null;
        }

        public IReadOnlyList<string> Options { get; }

        public string TrackNumberText => TrackNumber > 0 ? TrackNumber.ToString() : string.Empty;    // Helper for binding

        private readonly IMediaService _mediaService;
        private readonly List<string> _options;
        private PlaybackItem? _item;
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

        protected MediaViewModel(MediaViewModel source)
        {
            _mediaService = source._mediaService;
            _item = source._item;
            _name = source._name;
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

        protected MediaViewModel(object source, IMediaService mediaService)
        {
            _mediaService = mediaService;
            Source = source;

            Location = string.Empty;
            _name = string.Empty;
            _mediaType = MediaPlaybackType.Unknown;
            _artists = Array.Empty<ArtistViewModel>();
            _options = new List<string>();
            Options = new ReadOnlyCollection<string>(_options);
        }

        public MediaViewModel(IMediaService mediaService, Media media)
            : this(media, mediaService)
        {
            _name = media.Meta(MetadataType.Title) ?? string.Empty;
            Location = media.Mrl;

            // Media is already loaded, create PlaybackItem
            _loaded = true;
            _item = new PlaybackItem(media, media);
        }

        public virtual MediaViewModel Clone()
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

        public void UpdateMediaType()
        {
            // Update media type when it was previously set Unknown. Usually when source is an URI.
            // We don't want to init PlaybackItem just for this.
            if (MediaType == MediaPlaybackType.Unknown && _item is { VideoTracks.Count: 0 })
                MediaType = MediaPlaybackType.Music;
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

        public virtual Task LoadDetailsAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task LoadThumbnailAsync()
        {
            return Task.CompletedTask;
        }
    }
}