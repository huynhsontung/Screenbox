using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;

namespace Screenbox.Services
{
    internal static class LogService
    {
        public static void Log(object message, [CallerMemberName] string source = default)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source}]: {message}");
        }

        public static void RegisterLibVLCLogging(LibVLC libVLC)
        {
            libVLC.Log -= LibVLC_Log;
            libVLC.Log += LibVLC_Log;
        }

        private static void LibVLC_Log(object sender, LogEventArgs e)
        {
            if (e.Level == LogLevel.Debug) return;
            Log(e.FormattedLog, "LibVLC");
        }
    }
}
