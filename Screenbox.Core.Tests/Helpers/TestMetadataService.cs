using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public sealed class TestMetadataService : ISourceMetadataService, IPlaybackMetadataService
{
    private readonly MediaInfo? _result;

    public int CallCount { get; private set; }

    public IMediaSource? LastSource { get; private set; }

    public IPlaybackItem? LastItem { get; private set; }

    public TestMetadataService(MediaInfo? result = null)
    {
        _result = result;
    }

    public Task<MediaInfo?> GetMediaInfoAsync(IMediaSource source, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastSource = source;
        return Task.FromResult(_result);
    }

    public Task<MediaInfo?> GetMediaInfoAsync(IPlaybackItem item, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastItem = item;
        return Task.FromResult(_result);
    }

    public Task<(string? Artist, TimeSpan Duration)> GetSummaryPropertiesAsync(
        IMediaSource source,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastSource = source;
        string? artist = string.IsNullOrEmpty(_result?.MusicProperties.Artist)
            ? null
            : _result.MusicProperties.Artist;
        TimeSpan duration = _result is null
            ? TimeSpan.Zero
            : _result.MusicProperties.Duration > TimeSpan.Zero
                ? _result.MusicProperties.Duration
                : _result.VideoProperties.Duration;
        return Task.FromResult((artist, duration));
    }
}
