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
    /// <inheritdoc/>
    public event EventHandler? CastingEnded;

    /// <summary>Application ID for the Google Default Media Receiver.</summary>
    private const string DefaultMediaReceiverId = "CC1AD845";

    private readonly IMediaStreamingService _streamingService;
    private ChromecastClient? _client;

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
    public async Task<bool> ConnectAndCastAsync(Renderer renderer, PlaybackItem item, TimeSpan startPosition)
    {
        try
        {
            // Tear down any existing session before starting a new one.
            await StopCastingAsync();

            // Resolve the media URL (or start local HTTP server for local files).
            Uri? streamUrl = await _streamingService.StartStreamAsync(item);
            if (streamUrl is null)
            {
                return false;
            }

            _client = new ChromecastClient();
            _client.Disconnected += OnClientDisconnected;

            await _client.ConnectChromecast(renderer.Target);
            await _client.LaunchApplicationAsync(DefaultMediaReceiverId);

            var media = new Media
            {
                ContentUrl = streamUrl.ToString(),
                ContentType = GetContentType(item),
                // BUFFERED allows the Chromecast to seek; LIVE would disable seeking.
                StreamType = StreamType.Buffered,
            };

            // Pass the start time so the Chromecast begins playback at the right position.
            await _client.MediaChannel.LoadAsync(media, autoPlay: true);

            // Seek to the requested start position after the media is loaded.
            if (startPosition > TimeSpan.Zero)
            {
                await _client.MediaChannel.SeekAsync(startPosition.TotalSeconds);
            }

            return true;
        }
        catch (Exception)
        {
            // Clean up partially-started session on failure.
            await StopCastingAsync();
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task StopCastingAsync()
    {
        // Always stop the local HTTP stream regardless of client state.
        _streamingService.StopStream();

        if (_client is null)
        {
            return;
        }

        _client.Disconnected -= OnClientDisconnected;

        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception)
        {
            // Ignore disconnect errors — the device may already be unreachable.
        }
        finally
        {
            _client = null;
        }
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raised by SharpCaster when the connection to the Chromecast is lost.
    /// Stops the local stream and notifies subscribers via <see cref="CastingEnded"/>.
    /// </summary>
    private void OnClientDisconnected(object sender, EventArgs e)
    {
        _streamingService.StopStream();
        _client = null;
        CastingEnded?.Invoke(this, EventArgs.Empty);
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

