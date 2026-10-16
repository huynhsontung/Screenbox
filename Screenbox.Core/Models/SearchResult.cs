using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Represents the results of a search query, including matching songs, videos,
/// artists, albums, and playlists. 
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SearchResult"/> class with the
    /// results for a specified search query.
    /// </summary>
    /// <param name="query">The search text used to produce these results.</param>
    /// <param name="songs">The sequence of matching songs.</param>
    /// <param name="videos">The sequence of matching videos.</param>
    /// <param name="artists">The sequence of matching artists.</param>
    /// <param name="albums">The sequence of matching albums.</param>
    /// <param name="playlists">The sequence of matching playlists.</param>
    public SearchResult(
        string query,
        IReadOnlyList<MediaViewModel> songs,
        IReadOnlyList<MediaViewModel> videos,
        IReadOnlyList<ArtistViewModel> artists,
        IReadOnlyList<AlbumViewModel> albums,
        IReadOnlyList<PlaylistViewModel> playlists)
    {
        Query = query;
        Songs = songs;
        Videos = videos;
        Artists = artists;
        Albums = albums;
        Playlists = playlists;
    }

    public string Query { get; }

    public IReadOnlyList<MediaViewModel> Songs { get; }

    public IReadOnlyList<MediaViewModel> Videos { get; }

    public IReadOnlyList<ArtistViewModel> Artists { get; }

    public IReadOnlyList<AlbumViewModel> Albums { get; }

    public IReadOnlyList<PlaylistViewModel> Playlists { get; }

    public bool HasItems => Songs.Count > 0 || Videos.Count > 0 || Artists.Count > 0 || Albums.Count > 0 || Playlists.Count > 0;
}
