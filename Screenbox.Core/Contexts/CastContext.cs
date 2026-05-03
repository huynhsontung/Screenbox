#nullable enable

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Sharpcaster;

namespace Screenbox.Core.Contexts;

public sealed partial class CastContext : ObservableObject
{
    [ObservableProperty]
    private RendererWatcher? _rendererWatcher;

    [ObservableProperty]
    private Renderer? _activeRenderer;

    /// <summary>The active SharpCaster client for the current cast session, or <c>null</c> when not casting.</summary>
    public ChromecastClient? Client { get; set; }

    /// <summary>Raised when the active cast session ends unexpectedly.</summary>
    public event EventHandler? CastingEnded;

    internal void RaiseCastingEnded() => CastingEnded?.Invoke(this, EventArgs.Empty);
}
