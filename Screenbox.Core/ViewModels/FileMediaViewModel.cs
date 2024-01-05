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
    private Task _loadTask;
    private Task _loadThumbnailTask;

    public FileMediaViewModel(IFilesService filesService, IMediaService mediaService,
        AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, StorageFile file)
        : base(file, mediaService, albumFactory, artistFactory)
    {
        _filesService = filesService;
        _loadTask = Task.CompletedTask;
        _loadThumbnailTask = Task.CompletedTask;

        Name = file.Name;
        MediaInfo.MediaType = GetMediaTypeForFile(file);
        Location = file.Path;
        Id = file.Path;
        File = file;
    }

    private FileMediaViewModel(FileMediaViewModel source) : base(source)
    {
        _filesService = source._filesService;
        _loadTask = source._loadTask;
        _loadThumbnailTask = source._loadThumbnailTask;
        File = source.File;
    }

    public override MediaViewModel Clone()
    {
        return new FileMediaViewModel(this);
    }

    public override Task LoadDetailsAsync()
    {
        if (!_loadTask.IsCompleted) return _loadTask;
        base.LoadDetailsAsync();
        _loadTask = LoadDetailsInternalAsync();
        return _loadTask;
    }

    private async Task LoadDetailsInternalAsync()
    {
        if (!File.IsAvailable) return;
        string[] additionalPropertyKeys = { SystemProperties.Title };

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
