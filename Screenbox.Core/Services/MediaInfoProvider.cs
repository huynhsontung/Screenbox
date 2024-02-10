#nullable enable

using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Screenbox.Core.Services;
public class MediaInfoProvider : IMediaInfoProvider
{
    public async Task<MediaInfo> GetMediaInfoAsync(StorageFile file)
    {
        if (!file.IsAvailable) return new MediaInfo();

        try
        {
            BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            MediaPlaybackType mediaType = GetMediaTypeForFile(file);
            switch (mediaType)
            {
                case MediaPlaybackType.Video:
                    VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
                    return new MediaInfo(basicProperties, videoProperties);
                case MediaPlaybackType.Music:
                    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
                    return new MediaInfo(basicProperties, musicProperties);
            }
        }
        catch (Exception e)
        {
            // System.Exception: The RPC server is unavailable.
            if (e.HResult != unchecked((int)0x800706BA))
                LogService.Log(e);
        }

        return new MediaInfo();
    }

    public Task<MediaInfo> GetMediaInfoAsync(Uri uri)
    {
        throw new NotImplementedException();
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