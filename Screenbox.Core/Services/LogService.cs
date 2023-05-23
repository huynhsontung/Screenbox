#nullable enable

using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

#if !DEBUG
using Microsoft.AppCenter.Crashes;
#endif

namespace Screenbox.Core.Services
{
    public static class LogService
    {
        public static void Log(object? message, [CallerMemberName] string? source = default)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source}]: {message}");
#if !DEBUG
            if (message is Exception e)
            {
                Crashes.TrackError(e);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void RegisterLibVlcLogging(LibVLC libVlc)
        {
            libVlc.Log -= LibVLC_Log;
            libVlc.Log += LibVLC_Log;
        }

        private static void LibVLC_Log(object sender, LogEventArgs e)
        {
            Log(e.FormattedLog, "LibVLC");
        }
    }
}
