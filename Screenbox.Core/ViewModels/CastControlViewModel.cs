#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Contexts;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Sentry;
using Sharpcaster;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class CastControlViewModel : ObservableObject
{
    public ObservableCollection<Renderer> Renderers { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CastCommand))]
    private Renderer? _selectedRenderer;

    [ObservableProperty] private Renderer? _castingDevice;
    [ObservableProperty] private bool _isCasting;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayerContext _playerContext;
    private readonly CastContext _castContext;
    private readonly ICastService _castService;
    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>
    /// Stores the local player's position at the moment casting started so it can be
    /// restored when the session ends or is manually stopped.
    /// </summary>
    private TimeSpan _positionBeforeCast;

    public CastControlViewModel(PlayerContext playerContext, CastContext castContext, ICastService castService)
    {
        _playerContext = playerContext;
        _castContext = castContext;
        _castService = castService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Renderers = new ObservableCollection<Renderer>();

        // Subscribe to the natural-end event permanently. The handler is idempotent
        // because it checks IsCasting before acting.
        _castContext.CastingNaturallyEnded += OnCastingNaturallyEnded;
    }

    /// <summary>
    /// Starts device discovery if not already casting.
    /// No longer requires a media player reference because SharpCaster's
    /// <see cref="RendererWatcher"/> does its own mDNS scanning.
    /// </summary>
    public void StartDiscovering()
    {
        if (IsCasting) return;
        if (_castContext.RendererWatcher is not null) return;

        var watcher = _castService.CreateRendererWatcher();
        _castContext.RendererWatcher = watcher;
        watcher.RendererFound += RendererWatcherOnRendererFound;
        watcher.RendererLost += RendererWatcherOnRendererLost;
        watcher.Start();
    }

    /// <summary>Stops device discovery and clears the renderer list.</summary>
    public void StopDiscovering()
    {
        var watcher = _castContext.RendererWatcher;
        if (watcher is not null)
        {
            watcher.RendererFound -= RendererWatcherOnRendererFound;
            watcher.RendererLost -= RendererWatcherOnRendererLost;
            watcher.Stop();
            watcher.Dispose();
            _castContext.RendererWatcher = null;
        }

        SelectedRenderer = null;
        Renderers.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanCast))]
    private async Task CastAsync()
    {
        if (SelectedRenderer is null || MediaPlayer is null) return;

        PlaybackItem? item = MediaPlayer.PlaybackItem;
        if (item is null) return;

        SentrySdk.AddBreadcrumb(
            "Start casting",
            category: "command",
            type: "user",
            data: new Dictionary<string, string>
            {
                { "rendererHash", SelectedRenderer.Name.GetHashCode().ToString() },
                { "rendererType", SelectedRenderer.Type },
                { "canRenderAudio", SelectedRenderer.CanRenderAudio.ToString() },
                { "canRenderVideo", SelectedRenderer.CanRenderVideo.ToString() },
            });

        // Record the current position so it can be restored when casting ends.
        _positionBeforeCast = MediaPlayer.Position;

        // Pause local playback while the Chromecast streams independently.
        MediaPlayer.Pause();

        ChromecastClient? client = await _castService.ConnectAndCastAsync(SelectedRenderer, item, _positionBeforeCast);

        if (client is not null)
        {
            AttachClient(client);
            _castContext.ActiveRenderer = SelectedRenderer;
            _castContext.IsCasting = true;
            CastingDevice = SelectedRenderer;
            IsCasting = true;
            StopDiscovering();
        }
        else
        {
            // Connection failed — resume local playback.
            MediaPlayer.Play();
        }
    }

    private bool CanCast() => SelectedRenderer is { IsAvailable: true };

    [RelayCommand]
    private async Task StopCastingAsync()
    {
        SentrySdk.AddBreadcrumb("Stop casting", category: "command", type: "user");

        ChromecastClient? client = DetachClient();
        await _castService.StopCastingAsync(client);

        RestorePlaybackAfterCastingEnds();

        StartDiscovering();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles the case where the Chromecast device naturally finishes playback (IDLE/FINISHED,
    /// IDLE/ERROR, or IDLE/CANCELLED). Cleans up the cast session and restores local playback
    /// in the same way as an explicit stop.
    /// </summary>
    private void OnCastingNaturallyEnded(object sender, EventArgs e)
    {
        // This event is already marshalled to the UI thread by CastContext.
        // Guard against duplicate calls (e.g., if both CastingNaturallyEnded and Disconnected fire).
        if (!IsCasting) return;

        ChromecastClient? client = DetachClient();

        // Fire-and-forget: the media already ended so StopAsync is a no-op on the receiver,
        // but we still need to disconnect and stop the local HTTP server cleanly.
        _ = _castService.StopCastingAsync(client);

        RestorePlaybackAfterCastingEnds();
        StartDiscovering();
    }

    private void OnClientDisconnected(object sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            ChromecastClient? disconnectedClient = sender as ChromecastClient;

            // Detach handlers before stopping so repeated disconnect callbacks are ignored.
            ChromecastClient? activeClient = DetachClient();

            await _castService.StopCastingAsync(activeClient ?? disconnectedClient);
            RestorePlaybackAfterCastingEnds();
            StartDiscovering();
        });
    }

    private void RendererWatcherOnRendererLost(object sender, RendererLostEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Renderers.Remove(e.Renderer);
            if (SelectedRenderer == e.Renderer) SelectedRenderer = null;
        });
    }

    private void RendererWatcherOnRendererFound(object sender, RendererFoundEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => Renderers.Add(e.Renderer));
    }

    private void AttachClient(ChromecastClient client)
    {
        ChromecastClient? previousClient = _castContext.Client;
        if (previousClient is not null)
        {
            previousClient.Disconnected -= OnClientDisconnected;
        }

        _castContext.Client = client;
        client.Disconnected += OnClientDisconnected;
    }

    private ChromecastClient? DetachClient()
    {
        ChromecastClient? client = _castContext.Client;
        if (client is not null)
        {
            client.Disconnected -= OnClientDisconnected;
            _castContext.Client = null;
        }

        return client;
    }

    private void RestorePlaybackAfterCastingEnds()
    {
        _castContext.IsCasting = false;
        _castContext.ActiveRenderer = null;
        CastingDevice = null;
        IsCasting = false;

        // Resume local playback from the last known Chromecast position so the user
        // can continue watching seamlessly. Fall back to the pre-cast position if no
        // cast position has been reported yet.
        if (MediaPlayer is not null)
        {
            double castPositionSeconds = _castContext.CastPosition;
            MediaPlayer.Position = castPositionSeconds > 0
                ? TimeSpan.FromSeconds(castPositionSeconds)
                : _positionBeforeCast;
            MediaPlayer.Play();
        }
    }
}

