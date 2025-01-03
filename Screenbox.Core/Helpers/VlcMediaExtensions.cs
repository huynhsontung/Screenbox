using LibVLCSharp.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Screenbox.Core.Helpers;
internal static class VlcMediaExtensions
{
    public static async Task ParseAsync(this Media media, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // Check if media is already parsed
        if (media.CheckParsed() && media.ParsedStatus is MediaParsedStatus.Done or MediaParsedStatus.Failed)
            return;

        await media.Parse(MediaParseOptions.ParseNetwork, (int)timeout.TotalMilliseconds, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Media may not be parsed even after calling Parse()
        // This can happen if the media is being open at the same time.
        if (media.CheckParsed() && media.ParsedStatus != MediaParsedStatus.Skipped)
            return;

        // Wait for the ParsedStatus to change again.
        TaskCompletionSource<MediaParsedStatus> tsc = new();
        Task task = tsc.Task;

        media.ParsedChanged += MediaOnParsedChanged;
        try
        {
            if (await Task.WhenAny(tsc.Task, Task.Delay(timeout, cancellationToken)) != task)
            {
                tsc.SetCanceled();
            }
        }
        finally
        {
            media.ParsedChanged -= MediaOnParsedChanged;
        }

        return;

        void MediaOnParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            tsc.TrySetResult(e.ParsedStatus);
        }
    }

    public static async Task WaitForParsed(this Media media, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (media.CheckParsed()) return;
        TaskCompletionSource<bool> tcs = new();
        Task task = tcs.Task;

        media.ParsedChanged += OnMediaOnParsedChanged;
        try
        {
            if (await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken)) != task)
            {
                tcs.SetCanceled();
            }
        }
        finally
        {
            media.ParsedChanged -= OnMediaOnParsedChanged;
        }

        return;

        void OnMediaOnParsedChanged(object sender, MediaParsedChangedEventArgs args)
        {
            tcs.TrySetResult(args.ParsedStatus == MediaParsedStatus.Done);
        }
    }

    public static bool CheckParsed(this Media media) =>
        media.IsParsed || media.ParsedStatus != 0 || media.State == VLCState.Playing;
}
