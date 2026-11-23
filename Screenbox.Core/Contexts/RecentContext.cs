using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Context for holding the application-wide recent media items.
/// </summary>
public sealed partial class RecentContext : ObservableRecipient
{
    /// <summary>
    /// The maximum number of items retained in the recent list.
    /// </summary>
    public const int MaxRecentItems = 100;

    public RecentContext(IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
    }

    /// <summary>
    /// Gets or sets the snapshot of recent media items.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial IReadOnlyList<IRecentItem> Recent { get; set; } = Array.Empty<IRecentItem>();
}
