using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Screenbox.Core.Services;

public static class LogService
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public static void Log(object? message, [CallerMemberName] string? source = default)
    {
        ILogger logger = string.IsNullOrWhiteSpace(source)
            ? _loggerFactory.CreateLogger(typeof(LogService).FullName!)
            : _loggerFactory.CreateLogger(source);

        if (message is Exception exception)
        {
            logger.LogError(exception, "Exception captured in {Source}.", source ?? nameof(LogService));
            return;
        }

        logger.LogInformation("{Message}", message);
    }

    [Conditional("DEBUG")]
    public static void RegisterLibVlcLogging(LibVLC libVlc)
    {
        libVlc.Log -= LibVLC_Log;
        libVlc.Log += LibVLC_Log;
    }

    private static void LibVLC_Log(object? sender, LogEventArgs e)
    {
        Log(e.FormattedLog, "LibVLC");
    }
}
