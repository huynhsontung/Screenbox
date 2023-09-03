#nullable enable

using LibVLCSharp.Shared;
using System;
using Windows.Storage;

namespace Screenbox.Core.Services;

public interface IMediaService
{
    Media CreateMedia(object source, params string[] options);
    Media CreateMedia(string source, params string[] options);
    Media CreateMedia(IStorageFile source, params string[] options);
    Media CreateMedia(Uri source, params string[] options);
    void DisposeMedia(Media media);
}