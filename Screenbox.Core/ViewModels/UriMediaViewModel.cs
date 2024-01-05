#nullable enable

using Screenbox.Core.Factories;
using Screenbox.Core.Services;
using System;
using System.Linq;

namespace Screenbox.Core.ViewModels;
public sealed class UriMediaViewModel : MediaViewModel
{
    public Uri Uri { get; }

    public UriMediaViewModel(IMediaService mediaService, AlbumViewModelFactory albumFactory, ArtistViewModelFactory artistFactory, Uri uri, string id = "")
        : base(uri, mediaService, albumFactory, artistFactory)
    {
        Name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        Location = uri.ToString();
        Id = string.IsNullOrEmpty(id) ? Location : id;
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
