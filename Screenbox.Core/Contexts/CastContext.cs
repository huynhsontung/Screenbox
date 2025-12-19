#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    private RendererWatcher? _rendererWatcher;

    [ObservableProperty]
    private Renderer? _activeRenderer;
}
