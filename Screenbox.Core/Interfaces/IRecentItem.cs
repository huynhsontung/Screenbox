using System.ComponentModel;

namespace Screenbox.Core;

/// <summary>
/// Represents an item displayed in the Recent Media list on the Home page.
/// </summary>
public interface IRecentItem : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the display name of the recent item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this recent item is currently playing.
    /// </summary>
    bool IsPlaying { get; }
}
