using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace Screenbox.Core.Factories;

public sealed class PlaylistViewModelFactory : IPlaylistViewModelFactory
{
    private readonly IPlaylistService _playlistService;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly ILogger<PlaylistViewModel> _logger;

    public PlaylistViewModelFactory(
        IPlaylistService playlistService,
        MediaViewModelFactory mediaFactory,
        ILogger<PlaylistViewModel> logger)
    {
        _playlistService = playlistService;
        _mediaFactory = mediaFactory;
        _logger = logger;
    }

    public PlaylistViewModel Create() => new(_playlistService, _mediaFactory, _logger);
}
