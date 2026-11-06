using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Windows.Media;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for the global play queue.
/// This is the single source of truth for play queue state consumed by ViewModels via data binding.
/// </summary>
/// <remarks>
/// Properties are written exclusively by <see cref="Coordinators.PlayQueueCoordinator"/>
/// as side effects of queue mutations. ViewModels should only read from this context;
/// they must not write to it directly.
/// </remarks>
public sealed partial class PlayQueueContext : ObservableObject
{
    /// <summary>
    /// The ordered list of media items in the current play queue.
    /// Created once and mutated in-place by <see cref="Coordinators.PlayQueueCoordinator"/>.
    /// </summary>
    public ObservableCollection<MediaViewModel> Items { get; } = new();

    /// <summary>The media item currently playing or selected for playback.</summary>
    [ObservableProperty] public partial MediaViewModel? CurrentItem { get; set; }

    /// <summary>The zero-based index of <see cref="CurrentItem"/> within <see cref="Items"/>, or -1 when nothing is active.</summary>
    [ObservableProperty] public partial int CurrentIndex { get; set; } = -1;

    /// <summary>The identifier of the currently active play queue session, or <see cref="Guid.Empty"/> when cleared.</summary>
    [ObservableProperty] public partial Guid ActiveSessionId { get; set; }

    /// <summary>The origin metadata of the currently active play queue session.</summary>
    [ObservableProperty] public partial PlaylistOrigin? ActiveOrigin { get; set; }

    /// <summary>Whether the active item in the play queue is currently playing.</summary>
    [ObservableProperty] public partial bool IsPlaying { get; set; }

    /// <summary>
    /// Whether shuffle mode is active.
    /// <para>
    /// ViewModels may bind this property two-way (e.g. from a toggle button).
    /// <see cref="Coordinators.PlayQueueCoordinator"/> subscribes to property-changed
    /// on this context to react to UI-initiated shuffle toggles.
    /// </para>
    /// </summary>
    [ObservableProperty] public partial bool ShuffleMode { get; set; }

    /// <summary>
    /// The current repeat mode for the play queue.
    /// <para>
    /// ViewModels may bind this property two-way (e.g. from a toggle button).
    /// <see cref="Coordinators.PlayQueueCoordinator"/> subscribes to property-changed
    /// on this context to react to UI-initiated repeat-mode changes.
    /// </para>
    /// </summary>
    [ObservableProperty] public partial MediaPlaybackAutoRepeatMode RepeatMode { get; set; }
}
