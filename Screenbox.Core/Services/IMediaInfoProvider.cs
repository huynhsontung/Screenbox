using Screenbox.Core.Models;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Screenbox.Core.Services;
public interface IMediaInfoProvider
{
    Task<MediaInfo> GetMediaInfoAsync(StorageFile file);
    Task<MediaInfo> GetMediaInfoAsync(Uri uri);
}