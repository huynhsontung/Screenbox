#nullable enable

using System;
using Windows.Storage;
using LibVLCSharp.Shared;

namespace Screenbox.Services;

internal interface IMediaService
{
    Media? CreateMedia(object source);
    Media? CreateMedia(string source);
    Media CreateMedia(IStorageFile source);
    Media CreateMedia(Uri source);
    void DisposeMedia(Media media);
}