#nullable enable

using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;
public sealed class FileMediaViewModel : MediaViewModel
{
    public StorageFile File { get; }

    private readonly IFilesService _filesService;
    private readonly AlbumViewModelFactory _albumFactory;
    private readonly ArtistViewModelFactory _artistFactory;
    private Task _loadTask;
    private Task _loadThumbnailTask;

    public FileMediaViewModel(IFilesService filesService, IMediaService mediaService,
        AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, StorageFile file)
        : base(file, mediaService)
    {
        _filesService = filesService;
        _albumFactory = albumFactory;
        _artistFactory = artistFactory;
        _loadTask = Task.CompletedTask;
        _loadThumbnailTask = Task.CompletedTask;

        Name = file.Name;
        MediaType = GetMediaTypeForFile(file);
        Location = file.Path;
        File = file;
    }

    private FileMediaViewModel(FileMediaViewModel source) : base(source)
    {
        _filesService = source._filesService;
        _albumFactory = source._albumFactory;
        _artistFactory = source._artistFactory;
        _loadTask = source._loadTask;
        _loadThumbnailTask = source._loadThumbnailTask;
        File = source.File;
    }

    public override MediaViewModel Clone()
    {
        return new FileMediaViewModel(this);
    }

    public async Task LoadTitleAsync()
    {
        string[] propertyKeys = { SystemProperties.Title };
        IDictionary<string, object> properties = await File.Properties.RetrievePropertiesAsync(propertyKeys);
        if (properties[SystemProperties.Title] is string name && !string.IsNullOrEmpty(name))
        {
            Name = name;
        }
    }

    public override Task LoadDetailsAsync()
    {
        if (!_loadTask.IsCompleted) return _loadTask;
        _loadTask = LoadDetailsInternalAsync();
        return _loadTask;
    }

    private async Task LoadDetailsInternalAsync()
    {
        if (!File.IsAvailable) return;
        string[] additionalPropertyKeys =
        {
            SystemProperties.Title,
            SystemProperties.Music.Artist,
            SystemProperties.Media.Duration
        };

        try
        {
            IDictionary<string, object> additionalProperties = await File.Properties.RetrievePropertiesAsync(additionalPropertyKeys);
            if (additionalProperties[SystemProperties.Title] is string name && !string.IsNullOrEmpty(name))
            {
                Name = name;
                if (MediaType == MediaPlaybackType.Video && name != File.Name)
                {
                    AltCaption = File.Name;
                }
            }

            if (additionalProperties[SystemProperties.Media.Duration] is ulong ticks and > 0)
            {
                TimeSpan duration = TimeSpan.FromTicks((long)ticks);
                Duration = duration;
                Caption = Humanizer.ToDuration(duration);
            }

            BasicProperties basicProperties = await File.GetBasicPropertiesAsync();

            switch (MediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties videoProperties = await File.Properties.GetVideoPropertiesAsync();
                    MediaInfo = new MediaInfo(basicProperties, videoProperties);
                    break;
                case MediaPlaybackType.Music:
                    MusicProperties musicProperties = await File.Properties.GetMusicPropertiesAsync();
                    MediaInfo = new MediaInfo(basicProperties, musicProperties);
                    Album ??= _albumFactory.AddSongToAlbum(this, musicProperties.Album, musicProperties.AlbumArtist, musicProperties.Year);

                    if (Artists.Length == 0)
                    {
                        string[] contributingArtists =
                            additionalProperties[SystemProperties.Music.Artist] as string[] ??
                            Array.Empty<string>();
                        Artists = _artistFactory.ParseArtists(contributingArtists, this);
                    }

                    if (string.IsNullOrEmpty(musicProperties.Artist))
                    {
                        AltCaption = musicProperties.Album;
                    }
                    else
                    {
                        Caption = musicProperties.Artist;
                        AltCaption = string.IsNullOrEmpty(musicProperties.Album)
                            ? musicProperties.Artist
                            : $"{musicProperties.Artist} – {musicProperties.Album}";
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

    public override Task LoadThumbnailAsync()
    {
        if (!_loadThumbnailTask.IsCompleted) return _loadThumbnailTask;
        _loadThumbnailTask = LoadThumbnailInternalAsync();
        return _loadThumbnailTask;
    }

    private async Task LoadThumbnailInternalAsync()
    {
        if (Thumbnail == null)
        {
            StorageItemThumbnail? source = ThumbnailSource = await _filesService.GetThumbnailAsync(File);
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
