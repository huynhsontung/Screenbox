#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using LibVLCSharp.Shared;
using Microsoft.AppCenter.Crashes;
using Sentry;
using Sentry.Protocol;

namespace Screenbox.Core.Services
{
    public static class LogService
    {
        public static void Log(object? message, [CallerMemberName] string? source = default)
        {
            Debug.WriteLine($"[{DateTime.Now.ToString(CultureInfo.CurrentCulture)} - {source}]: {message}");
            if (message is Exception e) TrackError(e);
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

        [Conditional("DEBUG")]
        private static void TrackError(Exception e)
        {
            if (e.Data.Count > 0)
            {
                Dictionary<string, string> dict = new(e.Data.Count);
                foreach (DictionaryEntry entry in e.Data)
                {
                    if (entry.Value == null || entry.Key == null) return;
                    dict[entry.Key.ToString()] = entry.Value.ToString();
                }

                Crashes.TrackError(e, dict);
            }
            else
            {
                Crashes.TrackError(e);
            }

            e.Data[Mechanism.HandledKey] = true;
            SentrySdk.CaptureException(e);
        }
    }
}
