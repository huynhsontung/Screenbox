#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;

namespace Screenbox.Core.Contexts;

public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    public partial RendererWatcher? RendererWatcher { get; set; }

    [ObservableProperty]
    public partial Renderer? ActiveRenderer { get; set; }
}
