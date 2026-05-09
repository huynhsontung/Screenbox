#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Events;
using Screenbox.Core.Contexts;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Sentry;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class CastControlViewModel : ObservableObject
{
    public ObservableCollection<ICastDevice> Renderers { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CastCommand))]
    private ICastDevice? _selectedRenderer;

    [ObservableProperty] private ICastDevice? _castingDevice;
    [ObservableProperty] private bool _isCasting;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayerContext _playerContext;
    private readonly CastContext _castContext;
    private readonly ICastService _castService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly List<ICastDeviceLocator> _locators;

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
        Renderers = new ObservableCollection<ICastDevice>();
        _locators = new List<ICastDeviceLocator>();

        // Subscribe to the natural-end event permanently. The handler is idempotent
        // because it checks IsCasting before acting.
        _castContext.CastingNaturallyEnded += OnCastingNaturallyEnded;
    }

    /// <summary>
    /// Starts device discovery if not already casting.
    /// Creates fresh locators for each supported protocol and starts them.
    /// </summary>
    public void StartDiscovering()
    {
        if (IsCasting) return;
        if (_locators.Count > 0) return;

        var locators = _castService.CreateLocators();
        foreach (ICastDeviceLocator locator in locators)
        {
            locator.DeviceFound += OnDeviceFound;
            locator.DeviceLost += OnDeviceLost;
            locator.Start();
            _locators.Add(locator);
        }
    }

    /// <summary>Stops device discovery and clears the renderer list.</summary>
    public void StopDiscovering()
    {
        foreach (ICastDeviceLocator locator in _locators)
        {
            locator.DeviceFound -= OnDeviceFound;
            locator.DeviceLost -= OnDeviceLost;
            locator.Stop();
            locator.Dispose();
        }

        _locators.Clear();
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
                { "rendererType", SelectedRenderer.Type.ToString() },
            });

        // Record the current position so it can be restored when casting ends.
        _positionBeforeCast = MediaPlayer.Position;

        // Pause local playback while the remote device streams independently.
        MediaPlayer.Pause();

        ICastSession? session = await _castService.ConnectAndCastAsync(SelectedRenderer, item, _positionBeforeCast);

        if (session is not null)
        {
            AttachSession(session);
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

        ICastSession? session = DetachSession();
        await _castService.DisconnectAsync(session);

        RestorePlaybackAfterCastingEnds();

        StartDiscovering();
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles the case where the cast device naturally finishes playback or is unexpectedly
    /// disconnected. Cleans up the cast session and restores local playback.
    /// </summary>
    private void OnCastingNaturallyEnded(object sender, EventArgs e)
    {
        // This event is already marshalled to the UI thread by CastContext.
        // Guard against duplicate calls (e.g., if both PlaybackEnded and Disconnected fire).
        if (!IsCasting) return;

        ICastSession? session = DetachSession();

        // Fire-and-forget: stop the local HTTP stream cleanly.
        _ = _castService.DisconnectAsync(session);

        RestorePlaybackAfterCastingEnds();
        StartDiscovering();
    }

    private void OnDeviceLost(object sender, CastDeviceRemovedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Renderers.Remove(e.Device);
            if (SelectedRenderer == e.Device) SelectedRenderer = null;
        });
    }

    private void OnDeviceFound(object sender, CastDeviceFoundEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => Renderers.Add(e.Device));
    }

    private void AttachSession(ICastSession session)
    {
        _castContext.Session = session;
    }

    private ICastSession? DetachSession()
    {
        ICastSession? session = _castContext.Session;
        _castContext.Session = null;
        return session;
    }

    private void RestorePlaybackAfterCastingEnds()
    {
        _castContext.IsCasting = false;
        CastingDevice = null;
        IsCasting = false;

        // Resume local playback from the last known cast position so the user
        // can continue watching seamlessly.
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

