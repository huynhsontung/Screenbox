using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Backup data for shuffle functionality
/// </summary>
internal sealed class ShuffleBackup
{
    public List<MediaViewModel> OriginalItems { get; }
    public List<MediaViewModel> Removals { get; }

    public ShuffleBackup(List<MediaViewModel> originalItems, List<MediaViewModel>? removals = null)
    {
        OriginalItems = originalItems;
        Removals = removals ?? new List<MediaViewModel>();
    }
}
