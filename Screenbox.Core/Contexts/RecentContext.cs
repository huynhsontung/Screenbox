using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Context for holding the application-wide recent media items and their MRU token mappings.
/// </summary>
public sealed partial class RecentContext : ObservableObject
{
    /// <summary>
    /// Gets the collection of recent media items.
    /// </summary>
    public ObservableCollection<MediaViewModel> Recent { get; } = new();

    /// <summary>
    /// Gets the mapping from media location or path to MRU token.
    /// </summary>
    public Dictionary<string, string> PathToMruMappings { get; } = new();

    /// <summary>
    /// Gets the mapping from MRU token to media view model.
    /// </summary>
    public Dictionary<string, MediaViewModel> TokenToMediaMappings { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the recent media list has been loaded from storage.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoaded { get; set; }
}
