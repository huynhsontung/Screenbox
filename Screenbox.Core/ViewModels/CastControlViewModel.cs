#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AppCenter.Analytics;
using Screenbox.Core.Events;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class CastControlViewModel : ObservableObject
    {
        public ObservableCollection<Renderer> Renderers { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CastControlViewModel.CastCommand))]
        private Renderer? _selectedRenderer;

        [ObservableProperty] private Renderer? _castingDevice;
        [ObservableProperty] private bool _isCasting;

        private readonly ICastService _castService;
        private readonly DispatcherQueue _dispatcherQueue;

        public CastControlViewModel(ICastService castService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _castService = castService;
            _castService.RendererFound += CastServiceOnRendererFound;
            _castService.RendererLost += CastServiceOnRendererLost;
            Renderers = new ObservableCollection<Renderer>();
        }

        public void StartDiscovering()
        {
            if (IsCasting) return;
            _castService.Start();
        }

        public void StopDiscovering()
        {
            _castService.Stop();
            SelectedRenderer = null;
            Renderers.Clear();
        }

        [RelayCommand(CanExecute = nameof(CanCast))]
        private void Cast()
        {
            if (SelectedRenderer == null) return;
            _castService.SetActiveRenderer(SelectedRenderer);
            CastingDevice = SelectedRenderer;
            IsCasting = true;
            Analytics.TrackEvent("StartCasting", new Dictionary<string, string>
            {
                {"rendererHash", SelectedRenderer.Name.GetHashCode().ToString()},
                {"type", SelectedRenderer.Type},
                {"canRenderAudio", SelectedRenderer.CanRenderAudio.ToString()},
                {"canRenderVideo", SelectedRenderer.CanRenderVideo.ToString()},
            });
        }

        private bool CanCast() => SelectedRenderer is { IsAvailable: true };

        [RelayCommand]
        private void StopCasting()
        {
            _castService.SetActiveRenderer(null);
            IsCasting = false;
            StartDiscovering();
            Analytics.TrackEvent("StopCasting");
        }

        private void CastServiceOnRendererLost(object sender, RendererLostEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                Renderers.Remove(e.Renderer);
                if (SelectedRenderer == e.Renderer) SelectedRenderer = null;
            });
        }

        private void CastServiceOnRendererFound(object sender, RendererFoundEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() => Renderers.Add(e.Renderer));
        }
    }
}
