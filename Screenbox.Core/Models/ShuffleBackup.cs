using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;

/// <summary>
/// Backup data for shuffle functionality
/// </summary>
internal sealed class ShuffleBackup
{
    public List<QueueItemViewModel> OriginalItems { get; }
    public List<QueueItemViewModel> Removals { get; }

    public ShuffleBackup(List<QueueItemViewModel> originalItems, List<QueueItemViewModel>? removals = null)
    {
        OriginalItems = originalItems;
        Removals = removals ?? new List<QueueItemViewModel>();
    }
}
