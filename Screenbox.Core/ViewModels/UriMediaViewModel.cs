#nullable enable

using Screenbox.Core.Factories;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Core.ViewModels;
public sealed class UriMediaViewModel : MediaViewModel
{
    public Uri Uri { get; }

    public StorageFile? File { get; private set; }

    private readonly IFilesService _filesService;

    public UriMediaViewModel(IMediaService mediaService, IFilesService fileService,
        AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, Uri uri)
        : base(uri, mediaService, albumFactory, artistFactory)
    {
        _filesService = fileService;
        Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        Location = uri.OriginalString;  // Important. Must be the original string to be consistent with StorageFile.Path
        Id = uri.OriginalString;
        Uri = uri;
    }

    private UriMediaViewModel(UriMediaViewModel source) : base(source)
    {
        _filesService = source._filesService;
        File = source.File;
        Uri = source.Uri;
    }

    public override MediaViewModel Clone()
    {
        return new UriMediaViewModel(this);
    }

    public override async Task LoadDetailsAsync()
    {
        if (!Uri.IsFile)
        {
            await base.LoadDetailsAsync();
            return;
        }

        try
        {
            StorageFile file = await GetFileAsync();
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            switch (MediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    MediaInfo = new MediaInfo(basicProperties, videoProperties);
                    break;
                case MediaPlaybackType.Music:
                    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
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

    public override async Task LoadThumbnailAsync()
    {
        if (Thumbnail != null) return;
        if (!Uri.IsFile)
        {
            await base.LoadThumbnailAsync();
            return;
        }

        try
        {
            StorageFile file = await GetFileAsync();
            StorageItemThumbnail? source = ThumbnailSource = await _filesService.GetThumbnailAsync(file);
            if (source == null) return;
            BitmapImage image = new();
            await image.SetSourceAsync(ThumbnailSource);
            Thumbnail = image;
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public async Task<StorageFile> GetFileAsync()
    {
        // if (!Uri.IsFile) return null;
        return File ??= await StorageFile.GetFileFromPathAsync(Uri.OriginalString);  // Return how StorageFile saves the path
    }
}
