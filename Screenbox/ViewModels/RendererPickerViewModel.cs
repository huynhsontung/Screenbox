#nullable enable

using System.Collections.ObjectModel;
using Windows.System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class RendererPickerViewModel : ObservableObject
    {
        public ObservableCollection<Renderer> Renderers { get; }

        [ObservableProperty] private Renderer? _selectedRenderer;
        [ObservableProperty] private bool _isDiscovering;

        private readonly ICastService _castService;
        private readonly DispatcherQueue _dispatcherQueue;

        public RendererPickerViewModel(ICastService castService)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _castService = castService;
            _castService.RendererFound += CastServiceOnRendererFound;
            _castService.RendererLost += CastServiceOnRendererLost;
            Renderers = new ObservableCollection<Renderer>();
        }

        public void StartCasting()
        {
            if (_selectedRenderer == null) return;
            _castService.SetActiveRenderer(_selectedRenderer);
        }

        public void StartDiscovering()
        {
            _castService.Start();
            IsDiscovering = true;
        }

        public void StopDiscovering()
        {
            _castService.Stop();
            IsDiscovering = false;
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
