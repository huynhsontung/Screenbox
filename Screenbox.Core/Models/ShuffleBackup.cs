#nullable enable

using System.Collections.Generic;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core.Models;
/// <summary>
/// Backup data for shuffle functionality
/// </summary>
internal sealed class ShuffleBackup
{
    public List<MediaViewModel> OriginalPlaylist { get; }
    public List<MediaViewModel> Removals { get; }

    public ShuffleBackup(List<MediaViewModel> originalPlaylist, List<MediaViewModel>? removals = null)
    {
        OriginalPlaylist = originalPlaylist;
        Removals = removals ?? new List<MediaViewModel>();
    }
}
