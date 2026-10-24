using System;
using Windows.Storage;

namespace Screenbox.Core.MediaSources;

public interface IMediaSourceFactory
{
    IMediaSource Create(StorageFile file);

    IMediaSource Create(Uri uri);

    IMediaSource Create(string pathOrUri);
}
