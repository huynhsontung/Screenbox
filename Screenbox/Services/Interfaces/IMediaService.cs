#nullable enable

using System;
using Windows.Storage;
using Screenbox.Core;

namespace Screenbox.Services;

internal interface IMediaService
{
    MediaHandle? CreateMedia(object source);
    MediaHandle? CreateMedia(string source);
    MediaHandle? CreateMedia(IStorageFile source);
    MediaHandle? CreateMedia(Uri source);
}