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
        if (media.IsParsed && media.ParsedStatus is MediaParsedStatus.Done or MediaParsedStatus.Failed)
            return;

        await media.Parse(MediaParseOptions.ParseNetwork, (int)timeout.TotalMilliseconds, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Media may not be parsed even after calling Parse()
        // This can happen if the media is being open at the same time.
        if (media.IsParsed && media.ParsedStatus != MediaParsedStatus.Skipped)
            return;

        // Wait for the ParsedStatus to change again.
        TaskCompletionSource<MediaParsedStatus> tsc = new();
        Task task = tsc.Task;

        media.ParsedChanged += MediaOnParsedChanged;

        if (await Task.WhenAny(tsc.Task, Task.Delay(timeout, cancellationToken)) != task)
        {
            tsc.SetCanceled();
        }

        return;

        void MediaOnParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            tsc.TrySetResult(e.ParsedStatus);
        }
    }
}
