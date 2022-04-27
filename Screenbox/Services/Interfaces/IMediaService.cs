#nullable enable

using Screenbox.Core;

namespace Screenbox.Services;

internal interface IMediaService
{
    /// <summary>
    /// There can only be one active Media instance at a time.
    /// </summary>
    MediaHandle? CurrentMedia { get; }

    void SetActive(MediaHandle mediaHandle);
    MediaHandle? CreateMedia(object source);
}