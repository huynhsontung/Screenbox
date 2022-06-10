#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;

namespace Screenbox.Services
{
    internal static class LogService
    {
        public static void Log(object? message, [CallerMemberName] string? source = default)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source}]: {message}");
        }

        public static void RegisterLibVlcLogging(LibVLC libVlc)
        {
#if DEBUG
            libVlc.Log -= LibVLC_Log;
            libVlc.Log += LibVLC_Log;
#endif
        }

        private static void LibVLC_Log(object sender, LogEventArgs e)
        {
            Log(e.FormattedLog, "LibVLC");
        }
    }
}
