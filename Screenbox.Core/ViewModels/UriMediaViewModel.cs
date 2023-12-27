#nullable enable

using Screenbox.Core.Services;
using System;
using System.Linq;

namespace Screenbox.Core.ViewModels;
public sealed class UriMediaViewModel : MediaViewModel
{
    public Uri Uri { get; }

    public UriMediaViewModel(IMediaService mediaService, Uri uri)
        : base(uri, mediaService)
    {
        Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        Location = uri.ToString();
        Uri = uri;
    }

    private UriMediaViewModel(UriMediaViewModel source) : base(source)
    {
        Uri = source.Uri;
    }

    public override MediaViewModel Clone()
    {
        return new UriMediaViewModel(this);
    }
}
