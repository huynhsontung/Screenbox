using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public sealed class TestMediaProbeService : IMediaProbeService
{
    private readonly MediaInfo? _result;

    public int CallCount { get; private set; }

    public IMediaSource? LastSource { get; private set; }

    public IPlaybackItem? LastItem { get; private set; }

    public string[]? LastOptions { get; private set; }

    public TestMediaProbeService(MediaInfo? result = null)
    {
        _result = result;
    }

    public Task<MediaInfo?> ProbeMediaAsync(IMediaSource source, string[]? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastSource = source;
        LastOptions = options;
        return Task.FromResult(_result);
    }

    public Task<MediaInfo?> ProbeMediaAsync(IPlaybackItem item, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastItem = item;
        return Task.FromResult(_result);
    }
}
