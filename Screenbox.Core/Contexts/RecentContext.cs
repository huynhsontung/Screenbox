using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Context for holding the application-wide recent media items.
/// </summary>
public sealed partial class RecentContext : ObservableObject
{
    /// <summary>
    /// The maximum number of items retained in the recent list.
    /// </summary>
    public const int MaxRecentItems = 100;

    /// <summary>
    /// Gets the collection of recent media items.
    /// </summary>
    public ObservableCollection<RecentItemViewModel> Recent { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the recent media list has been loaded from storage.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoaded { get; set; }
}
