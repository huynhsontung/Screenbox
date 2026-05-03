#nullable enable

using System;
using Screenbox.Casting.Models;
using Screenbox.Casting.Chromecast;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Discovery;
using Screenbox.Casting.Services;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.Storage;

namespace Screenbox.Core.Services;

public sealed class CastService : ICastService
{
    private readonly ICastDeviceDiscovery _discovery;
    private readonly ICastCompatibilityAnalyzer _compatibilityAnalyzer;
    private readonly LocalMediaServer _localMediaServer;

    private ChromecastSession? _session;

    public CastService()
    {
        _discovery = new ChromecastMdnsDiscovery();
        _compatibilityAnalyzer = new BasicCastCompatibilityAnalyzer();
        _localMediaServer = new LocalMediaServer();
    }

    public RendererWatcher CreateRendererWatcher(IMediaPlayer player)
    {
        return new RendererWatcher(_discovery);
    }

    public bool SetActiveRenderer(IMediaPlayer player, Renderer? renderer)
    {
        try
        {
            if (renderer is null)
            {
                if (_session is not null)
                {
                    _session.DisconnectAsync().GetAwaiter().GetResult();
                    _session.DisposeAsync().GetAwaiter().GetResult();
                    _session = null;
                }

                return true;
            }

            if (renderer.TargetDevice is null)
            {
                return false;
            }

            _session?.DisconnectAsync().GetAwaiter().GetResult();
            _session?.DisposeAsync().GetAwaiter().GetResult();

            _session = new ChromecastSession(_compatibilityAnalyzer);
            _session.ConnectAsync(renderer.TargetDevice).GetAwaiter().GetResult();
            _session.LaunchDefaultReceiverAsync().GetAwaiter().GetResult();

            if (TryCreateMediaSource(player, out CastMediaSource? source))
            {
                CastCompatibilityResult result = _session.LoadAsync(source).GetAwaiter().GetResult();
                if (result.Compatibility == CastCompatibility.RequiresRemuxOrTranscode)
                {
                    _session.DisconnectAsync().GetAwaiter().GetResult();
                    _session.DisposeAsync().GetAwaiter().GetResult();
                    _session = null;
                    return false;
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool TryCreateMediaSource(IMediaPlayer player, out CastMediaSource? source)
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
                _localMediaServer.Start();
                source = new CastMediaSource(_localMediaServer.BuildFileUri(file.Path), LocalMediaServer.GetContentType(file.Path), title);
                return true;

            case string path when !string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path):
                _localMediaServer.Start();
                source = new CastMediaSource(_localMediaServer.BuildFileUri(path), LocalMediaServer.GetContentType(path), title);
                return true;

            default:
                return false;
        }
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
