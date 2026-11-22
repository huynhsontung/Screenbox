using System.ComponentModel;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Exposes observable active and playing state for media list UI binding.
/// Implemented by both <see cref="MediaViewModel"/> and <see cref="QueueItemViewModel"/>.
/// </summary>
public interface IMediaPlaybackState : INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the item is currently active in the play queue.
    /// </summary>
    bool IsMediaActive { get; }

    /// <summary>
    /// Gets a value indicating whether the item is currently playing.
    /// </summary>
    bool IsPlaying { get; }
}
