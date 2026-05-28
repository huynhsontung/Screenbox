using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Factories;

public sealed class PlaylistViewModelFactory : IPlaylistViewModelFactory
{
    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;

    public PlaylistViewModelFactory(IPlaylistService playlistService, MediaViewModelFactory mediaFactory)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
    }

    public PlaylistViewModel Create() => new(_playlistService, _mediaFactory);
}
