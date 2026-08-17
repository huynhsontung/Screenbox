using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class CastControlViewModel : ObservableObject
{
    public ObservableCollection<Renderer> Renderers { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CastCommand))]
    public partial Renderer? SelectedRenderer { get; set; }

    [ObservableProperty] public partial Renderer? CastingDevice { get; set; }
    [ObservableProperty] public partial bool IsCasting { get; set; }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly PlayerContext _playerContext;
    private readonly CastContext _castContext;
    private readonly ICastService _castService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<CastControlViewModel> _logger;

    public CastControlViewModel(PlayerContext playerContext, CastContext castContext, ICastService castService,
        ILogger<CastControlViewModel> logger)
    {
        _playerContext = playerContext;
        _castContext = castContext;
        _castService = castService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _logger = logger;
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
    private void Cast()
    {
        if (SelectedRenderer == null || MediaPlayer == null) return;
        _logger.LogInformation("Start casting. {RendererHash} {RendererType} {CanRenderAudio} {CanRenderVideo}",
            SelectedRenderer.Name.GetHashCode(),
            SelectedRenderer.Type,
            SelectedRenderer.CanRenderAudio,
            SelectedRenderer.CanRenderVideo);
        if (_castService.SetActiveRenderer(MediaPlayer, SelectedRenderer))
        {
            _castContext.ActiveRenderer = SelectedRenderer;
            CastingDevice = SelectedRenderer;
            IsCasting = true;
        }
    }

    private bool CanCast() => SelectedRenderer is { IsAvailable: true };

    [RelayCommand]
    private void StopCasting()
    {
        if (MediaPlayer == null) return;
        _logger.LogInformation("Stop casting.");
        _castService.SetActiveRenderer(MediaPlayer, null);
        _castContext.ActiveRenderer = null;
        IsCasting = false;
        StartDiscovering();
    }

    private void RendererWatcherOnRendererLost(object? sender, RendererLostEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Renderers.Remove(e.Renderer);
            if (SelectedRenderer == e.Renderer) SelectedRenderer = null;
        });
    }

    private void RendererWatcherOnRendererFound(object? sender, RendererFoundEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => Renderers.Add(e.Renderer));
    }
}
