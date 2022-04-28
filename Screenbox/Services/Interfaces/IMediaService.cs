#nullable enable

using System;
using Screenbox.Core;

namespace Screenbox.Services;

internal interface IMediaService
{
    public event EventHandler<MediaChangedEventArgs>? CurrentMediaChanged;

    /// <summary>
    /// There can only be one active Media instance at a time.
    /// </summary>
    MediaHandle? CurrentMedia { get; }

    void SetActive(MediaHandle mediaHandle);
    MediaHandle? CreateMedia(object source);
}