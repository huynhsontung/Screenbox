using System;
using System.Threading.Tasks;

namespace Screenbox.Services;

public interface ITelemetryService
{
    bool IsEnabled { get; }

    Task FlushAsync(TimeSpan? timeout = null);
}
