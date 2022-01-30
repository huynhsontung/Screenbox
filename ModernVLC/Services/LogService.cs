using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModernVLC.Services
{
    internal static class LogService
    {
        public static void Log(object message, [CallerMemberName] string source = default)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source}]: {message}");
        }

        public static void RegisterLogsHandler(this LibVLC libVLC)
        {
#if DEBUG
            libVLC.Log += LibVLC_Log;
#endif
        }

        private static void LibVLC_Log(object sender, LogEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - LibVLC]: {e.FormattedLog}");
        }
    }
}
