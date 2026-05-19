#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Holds the observable state of the application window, such as the current view mode.
/// Written by <see cref="Services.IWindowService"/> and observed by view models via the messenger.
/// </summary>
public sealed partial class WindowContext : ObservableRecipient
{
    /// <summary>
    /// Gets or sets the current window view mode (e.g., Default, FullScreen, Compact).
    /// Broadcasts a <see cref="CommunityToolkit.Mvvm.Messaging.Messages.PropertyChangedMessage{T}"/> via the
    /// messenger when changed, so view models can react without subscribing to a service event.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private WindowViewMode _viewMode;
}
