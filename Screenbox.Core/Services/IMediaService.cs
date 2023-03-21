#nullable enable

using System;
using Windows.Storage;
using LibVLCSharp.Shared;

namespace Screenbox.Core.Services;

public interface IMediaService
{
    Media? CreateMedia(object source);
    Media? CreateMedia(string source);
    Media CreateMedia(IStorageFile source);
    Media CreateMedia(Uri source);
    void DisposeMedia(Media media);
}