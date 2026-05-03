#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Casting.Contracts;
using Screenbox.Casting.Models;
using Screenbox.Casting.Services;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    private RendererWatcher? _rendererWatcher;

    [ObservableProperty]
    private Renderer? _activeRenderer;

    [ObservableProperty]
    private CastSessionState _sessionState;

    [ObservableProperty]
    private string? _lastError;

    [ObservableProperty]
    private ICastSession? _castSession;

    [ObservableProperty]
    private LocalMediaServer? _localMediaServer;
}
