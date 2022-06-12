#nullable enable

using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Screenbox.Core;
using Screenbox.Services;

namespace Screenbox.ViewModels
{
    internal partial class CastControlViewModel : ObservableObject
    {
        public ObservableCollection<Renderer> Renderers { get; }

        public IRelayCommand CastCommand { get; }

        [ObservableProperty] private Renderer? _selectedRenderer;
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
            CastCommand = new RelayCommand(Cast, CanCast);
            PropertyChanged += OnPropertyChanged;
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

        private void Cast()
        {
            if (_selectedRenderer == null) return;
            _castService.SetActiveRenderer(_selectedRenderer);
            CastingDevice = _selectedRenderer;
            IsCasting = true;
        }

        private bool CanCast() => _selectedRenderer != null && _selectedRenderer.IsAvailable;

        [ICommand]
        private void StopCasting()
        {
            _castService.SetActiveRenderer(null);
            IsCasting = false;
            StartDiscovering();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedRenderer))
            {
                CastCommand.NotifyCanExecuteChanged();
            }
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
