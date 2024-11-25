#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Screenbox.Core.ViewModels;
public partial class LivelyWallpaperPlayerViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>
{
    [ObservableProperty] private LivelyWallpaperModel? _source;
    [ObservableProperty] private bool _isLoading;

    private readonly ILivelyWallpaperService _livelyService;
    private readonly ISettingsService _settingsService;

    public LivelyWallpaperPlayerViewModel(ILivelyWallpaperService livelyService, ISettingsService settingsService)
    {
        _livelyService = livelyService;
        _settingsService = settingsService;

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        Source = message.NewValue;
    }

    public void SendError(string title, string message)
    {
        Messenger.Send(new ErrorMessage(title, message));
    }

    public async Task LoadAsync()
    {
        var allVisualizers = await _livelyService.GetAvailableVisualizersAsync();
        var activeVisualizerPath = _settingsService.LivelyActivePath;

        var selectedVisualizer =
            allVisualizers.FirstOrDefault(visualizer =>
                visualizer.Path.Equals(activeVisualizerPath, StringComparison.OrdinalIgnoreCase));

        Source = selectedVisualizer;
    }
}
