using System;
using System.Threading.Tasks;
using Screenbox.Helpers;
using Sentry;

namespace Screenbox.Services;

public sealed class SentryTelemetryService : ITelemetryService
{
    private readonly string _dsn = Secrets.SentryDsn;

    public bool IsEnabled => !string.IsNullOrEmpty(_dsn);

    public SentryTelemetryService()
    {
        if (!IsEnabled)
        {
            return;
        }

        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("device_family", DeviceInfoHelper.DeviceFamily);
        });
    }

    public Task FlushAsync(TimeSpan? timeout = null)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        var flushTimeout = timeout ?? TimeSpan.FromSeconds(_dsn.Contains("sentry.io", StringComparison.OrdinalIgnoreCase) ? 2 : 0.5);
        return SentrySdk.FlushAsync(flushTimeout);
    }
}
