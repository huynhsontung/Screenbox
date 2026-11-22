using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents an individual slot in the play queue wrapping a canonical <see cref="MediaViewModel"/>
/// with queue-slot-specific playback state and identity.
/// </summary>
public sealed partial class QueueItemViewModel : ObservableObject, IMediaContainer, IMediaPlaybackState
{
    /// <inheritdoc/>
    public MediaViewModel Media { get; }

    /// <inheritdoc cref="IMediaPlaybackState.IsMediaActive"/>
    [ObservableProperty]
    public partial bool IsMediaActive { get; set; }

    /// <inheritdoc cref="IMediaPlaybackState.IsPlaying"/>
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueItemViewModel"/> class for the specified media item.
    /// </summary>
    /// <param name="media">The canonical media view model represented by this queue slot.</param>
    public QueueItemViewModel(MediaViewModel media)
    {
        ArgumentNullException.ThrowIfNull(media);
        Media = media;
    }

    public override string ToString() => Media.ToString();
}
