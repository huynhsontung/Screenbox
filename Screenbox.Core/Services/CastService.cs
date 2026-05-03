#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Screenbox.Casting.Chromecast;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Discovery;
using Screenbox.Casting.Models;
using Screenbox.Casting.Services;
using Screenbox.Core.Contexts;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed class CastService : ICastService
{
    private readonly ICastDeviceDiscovery _discovery;
    private readonly ICastCompatibilityAnalyzer _compatibilityAnalyzer;

    public CastService()
    {
        _discovery = new ChromecastMdnsDiscovery();
        _compatibilityAnalyzer = new BasicCastCompatibilityAnalyzer();
    }

    public RendererWatcher CreateRendererWatcher(IMediaPlayer player)
    {
        return new RendererWatcher(_discovery);
    }

    public async Task<CastOperationResult> SetActiveRendererAsync(CastContext context, IMediaPlayer player, Renderer? renderer, CancellationToken cancellationToken = default)
    {
        try
        {
            if (renderer is null)
            {
                if (context.CastSession is not null)
                {
                    await context.CastSession.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    await context.CastSession.DisposeAsync().ConfigureAwait(false);
                    context.CastSession = null;
                }

                if (context.LocalMediaServer is not null)
                {
                    await context.LocalMediaServer.StopAsync().ConfigureAwait(false);
                    context.LocalMediaServer.Dispose();
                    context.LocalMediaServer = null;
                }

                return new CastOperationResult(true, CastSessionState.Disconnected);
            }

            if (renderer.TargetDevice is null)
            {
                return new CastOperationResult(false, CastSessionState.Faulted, "Renderer is unavailable.");
            }

            if (context.CastSession is not null)
            {
                await context.CastSession.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                await context.CastSession.DisposeAsync().ConfigureAwait(false);
                context.CastSession = null;
            }

            ChromecastSession session = new(_compatibilityAnalyzer);
            context.CastSession = session;

            await session.ConnectAsync(renderer.TargetDevice, cancellationToken).ConfigureAwait(false);
            await session.LaunchDefaultReceiverAsync(cancellationToken).ConfigureAwait(false);

            if (TryCreateMediaSource(context, player, out CastMediaSource? source))
            {
                CastCompatibilityResult result = await session.LoadAsync(source!, cancellationToken).ConfigureAwait(false);
                if (result.Compatibility == CastCompatibility.RequiresRemuxOrTranscode)
                {
                    await session.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    await session.DisposeAsync().ConfigureAwait(false);
                    context.CastSession = null;
                    return new CastOperationResult(false, CastSessionState.Faulted, result.Reason);
                }
            }

            return new CastOperationResult(true, session.State);
        }
        catch (Exception ex)
        {
            if (context.CastSession is not null)
            {
                try
                {
                    await context.CastSession.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    await context.CastSession.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Ignore cleanup failures after primary cast error.
                }

                context.CastSession = null;
            }

            return new CastOperationResult(false, CastSessionState.Faulted, ex.Message);
        }
    }

    public async Task<CastOperationResult> PlayAsync(CastContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.CastSession is null)
            {
                return new CastOperationResult(false, CastSessionState.Disconnected, "No active cast session.");
            }

            await context.CastSession.PlayAsync(cancellationToken).ConfigureAwait(false);
            return new CastOperationResult(true, context.CastSession.State);
        }
        catch (Exception ex)
        {
            return new CastOperationResult(false, CastSessionState.Faulted, ex.Message);
        }
    }

    public async Task<CastOperationResult> PauseAsync(CastContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.CastSession is null)
            {
                return new CastOperationResult(false, CastSessionState.Disconnected, "No active cast session.");
            }

            await context.CastSession.PauseAsync(cancellationToken).ConfigureAwait(false);
            return new CastOperationResult(true, context.CastSession.State);
        }
        catch (Exception ex)
        {
            return new CastOperationResult(false, CastSessionState.Faulted, ex.Message);
        }
    }

    public async Task<CastOperationResult> StopAsync(CastContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.CastSession is null)
            {
                return new CastOperationResult(false, CastSessionState.Disconnected, "No active cast session.");
            }

            await context.CastSession.StopAsync(cancellationToken).ConfigureAwait(false);
            return new CastOperationResult(true, context.CastSession.State);
        }
        catch (Exception ex)
        {
            return new CastOperationResult(false, CastSessionState.Faulted, ex.Message);
        }
    }

    private static bool TryCreateMediaSource(CastContext context, IMediaPlayer player, out CastMediaSource? source)
    {
        source = null;

        PlaybackItem? playbackItem = player.PlaybackItem;
        if (playbackItem is null)
        {
            return false;
        }

        string title = playbackItem.OriginalSource switch
        {
            IStorageFile file => file.Name,
            Uri uri => uri.Segments.Length > 0 ? uri.Segments[uri.Segments.Length - 1] : uri.Host,
            string value => System.IO.Path.GetFileName(value),
            _ => "Screenbox media",
        };

        switch (playbackItem.OriginalSource)
        {
            case Uri uri when uri.IsAbsoluteUri:
                source = new CastMediaSource(uri, InferContentType(uri), title);
                return true;

            case string value when Uri.TryCreate(value, UriKind.Absolute, out Uri? absoluteUri):
                source = new CastMediaSource(absoluteUri, InferContentType(absoluteUri), title);
                return true;

            case IStorageFile file when !string.IsNullOrWhiteSpace(file.Path):
                EnsureLocalServer(context);
                source = new CastMediaSource(context.LocalMediaServer!.BuildFileUri(file.Path), LocalMediaServer.GetContentType(file.Path), title);
                return true;

            case string path when !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path):
                EnsureLocalServer(context);
                source = new CastMediaSource(context.LocalMediaServer!.BuildFileUri(path), LocalMediaServer.GetContentType(path), title);
                return true;

            default:
                return false;
        }
    }

    private static void EnsureLocalServer(CastContext context)
    {
        context.LocalMediaServer ??= new LocalMediaServer();
        context.LocalMediaServer.Start();
    }

    private static string InferContentType(Uri uri)
    {
        if (uri.IsFile)
        {
            return LocalMediaServer.GetContentType(uri.LocalPath);
        }

        string absolutePath = uri.AbsolutePath ?? string.Empty;
        if (absolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return "application/vnd.apple.mpegurl";
        }

        if (absolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return "video/mp4";
        }

        if (absolutePath.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
        {
            return "audio/mp4";
        }

        if (absolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            return "audio/mpeg";
        }

        return "application/octet-stream";
    }
}
