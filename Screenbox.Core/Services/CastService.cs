#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Sharpcaster;
using Sharpcaster.Models.Media;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Windows.Storage;

namespace Screenbox.Core.Services;

/// <summary>
/// Implements <see cref="ICastService"/> using the SharpCaster library.
///
/// <para>
/// This service is transient and stateless from the app perspective. The caller owns
/// any active <see cref="ChromecastClient"/> instance and is responsible for tracking
/// client lifecycle between calls.
/// </para>
/// <para>
/// Casting flow:
/// <list type="number">
///   <item>Resolve the playback item to an HTTP URL via <see cref="IMediaStreamingService"/>.</item>
///   <item>Connect to the target <see cref="ChromecastReceiver"/> using <see cref="ChromecastClient"/>.</item>
///   <item>Launch the Default Media Receiver application on the device.</item>
///   <item>Send a LOAD command with the media URL, content type, and start position.</item>
/// </list>
/// </para>
/// </summary>
public sealed class CastService : ICastService
{
    /// <summary>Application ID for the Google Default Media Receiver.</summary>
    private const string DefaultMediaReceiverId = "CC1AD845";

    private readonly IMediaStreamingService _streamingService;

    public CastService(IMediaStreamingService streamingService)
    {
        _streamingService = streamingService;
    }

    /// <inheritdoc/>
    public RendererWatcher CreateRendererWatcher()
    {
        return new RendererWatcher();
    }

    /// <inheritdoc/>
    public async Task<ChromecastClient?> ConnectAndCastAsync(Renderer renderer, PlaybackItem item, TimeSpan startPosition)
    {
        ChromecastClient? client = null;

        try
        {
            // Resolve the media URL (or start local HTTP server for local files).
            Uri? streamUrl = await _streamingService.StartStreamAsync(item);
            if (streamUrl is null)
            {
                return null;
            }

            client = new ChromecastClient();

            await client.ConnectChromecast(renderer.Target);
            await client.LaunchApplicationAsync(DefaultMediaReceiverId);

            var media = new Media
            {
                ContentUrl = streamUrl.ToString(),
                ContentType = GetContentType(item),
                // BUFFERED allows the Chromecast to seek; LIVE would disable seeking.
                StreamType = StreamType.Buffered,
            };

            // Pass the start time so the Chromecast begins playback at the right position.
            await client.MediaChannel.LoadAsync(media, autoPlay: true);

            // Seek to the requested start position after the media is loaded.
            if (startPosition > TimeSpan.Zero)
            {
                await client.MediaChannel.SeekAsync(startPosition.TotalSeconds);
            }

            return client;
        }
        catch (Exception)
        {
            // Clean up partially-started session on failure.
            await StopCastingAsync(client);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task StopCastingAsync(ChromecastClient? client = null)
    {
        // Always stop the local HTTP stream regardless of client state.
        _streamingService.StopStream();

        if (client is null)
        {
            return;
        }

        try
        {
            await client.DisconnectAsync();
        }
        catch (Exception)
        {
            // Ignore disconnect errors — the device may already be unreachable.
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Infers the MIME content-type for a playback item from its source's file extension.</summary>
    private static string GetContentType(PlaybackItem item)
    {
        string? extension = item.OriginalSource switch
        {
            IStorageFile file => Path.GetExtension(file.Name),
            string path => Path.GetExtension(path),
            Uri uri => Path.GetExtension(uri.AbsolutePath),
            _ => null
        };

        return extension?.ToLowerInvariant() switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".mp3" => "audio/mpeg",
            ".m4a" or ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".wav" => "audio/wav",
            ".ogg" or ".opus" => "audio/ogg",
            _ => "video/mp4"
        };
    }
}

