#nullable enable

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

    public CastControlViewModel(PlayerContext playerContext, CastContext castContext, ICastService castService)
    {
        _playerContext = playerContext;
        _castContext = castContext;
        _castService = castService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Renderers = new ObservableCollection<Renderer>();
    }

    public void StartDiscovering()
    {
        if (IsCasting || MediaPlayer == null) return;

        var watcher = _castService.CreateRendererWatcher(MediaPlayer);
        _castContext.RendererWatcher = watcher;
        watcher.RendererFound += RendererWatcherOnRendererFound;
        watcher.RendererLost += RendererWatcherOnRendererLost;
        watcher.Start();
    }

    public void StopDiscovering()
    {
        var watcher = _castContext.RendererWatcher;
        if (watcher != null)
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
        if (SelectedRenderer == null || MediaPlayer == null) return;
        SentrySdk.AddBreadcrumb("Start casting", category: "command", type: "user", data: new Dictionary<string, string>
        {
            {"rendererHash", SelectedRenderer.Name.GetHashCode().ToString()},
            {"rendererType", SelectedRenderer.Type},
            {"canRenderAudio", SelectedRenderer.CanRenderAudio.ToString()},
            {"canRenderVideo", SelectedRenderer.CanRenderVideo.ToString()},
        });
        CastOperationResult result = await _castService.SetActiveRendererAsync(_castContext, MediaPlayer, SelectedRenderer);
        _castContext.SessionState = result.SessionState;
        _castContext.LastError = result.ErrorMessage;

        if (result.Succeeded)
        {
            _castContext.ActiveRenderer = SelectedRenderer;
            CastingDevice = SelectedRenderer;
            IsCasting = true;
        }
        else
        {
            _castContext.ActiveRenderer = null;
            CastingDevice = null;
            IsCasting = false;
        }
    }

    private bool CanCast() => SelectedRenderer is { IsAvailable: true };

    [RelayCommand]
    private async Task StopCastingAsync()
    {
        if (MediaPlayer == null) return;
        SentrySdk.AddBreadcrumb("Stop casting", category: "command", type: "user");
        CastOperationResult result = await _castService.SetActiveRendererAsync(_castContext, MediaPlayer, null);
        _castContext.SessionState = result.SessionState;
        _castContext.LastError = result.ErrorMessage;
        _castContext.ActiveRenderer = null;
        IsCasting = false;
        CastingDevice = null;
        StartDiscovering();
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
