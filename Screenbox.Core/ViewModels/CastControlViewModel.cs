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

        // React to the Chromecast going away (e.g., device turned off) while casting.
        _castContext.CastingEnded += OnCastingEnded;
    }

    /// <summary>
    /// Starts device discovery if not already casting.
    /// No longer requires a media player reference because SharpCaster's
    /// <see cref="RendererWatcher"/> does its own mDNS scanning.
    /// </summary>
    public void StartDiscovering()
    {
        if (IsCasting) return;

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

        bool success = await _castService.ConnectAndCastAsync(SelectedRenderer, item, _positionBeforeCast);

        if (success)
        {
            _castContext.ActiveRenderer = SelectedRenderer;
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

        await _castService.StopCastingAsync();

        _castContext.ActiveRenderer = null;
        IsCasting = false;

        // Resume local playback from where it was before casting started.
        if (MediaPlayer is not null)
        {
            MediaPlayer.Position = _positionBeforeCast;
            MediaPlayer.Play();
        }

        StartDiscovering();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles unexpected cast session termination (e.g., device disconnected).
    /// Resumes local playback and restarts discovery on the UI thread.
    /// </summary>
    private void OnCastingEnded(object sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _castContext.ActiveRenderer = null;
            IsCasting = false;

            if (MediaPlayer is not null)
            {
                MediaPlayer.Position = _positionBeforeCast;
                MediaPlayer.Play();
            }

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
}

