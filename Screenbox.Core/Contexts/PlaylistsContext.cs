#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Context for holding the application-wide playlists.
/// </summary>
public sealed partial class PlaylistsContext : ObservableObject
{
    /// <summary>
    /// Gets the collection of playlists.
    /// </summary>
    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();
}
