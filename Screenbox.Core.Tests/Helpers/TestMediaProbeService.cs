using System.Threading;
using System.Threading.Tasks;
using Screenbox.Core.Models;
using Screenbox.Core.Services;

namespace Screenbox.Core.Tests.Helpers;

public class TestMediaProbeService : IMediaProbeService
{
    private readonly MediaInfo? _result;

    public int CallCount { get; private set; }

    public TestMediaProbeService(MediaInfo? result = null)
    {
        _result = result;
    }

    public Task<MediaInfo?> ProbeMediaAsync(object source, string[]? options = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}
