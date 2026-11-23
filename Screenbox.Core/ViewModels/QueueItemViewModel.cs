using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Models;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents an individual slot in the play queue wrapping a canonical <see cref="MediaViewModel"/>
/// with queue-slot-specific playback state, origin metadata, and identity.
/// </summary>
public sealed partial class QueueItemViewModel : ObservableObject, IMediaContainer, IMediaPlaybackState
{
    /// <inheritdoc/>
    public MediaViewModel Media { get; }

    /// <summary>
    /// Gets or sets the container origin metadata describing where this queue slot came from
    /// (e.g. an album, artist, playlist, or folder/playlist file).
    /// <see langword="null"/> indicates an individually tracked item.
    /// </summary>
    public QueueOrigin? Origin { get; set; }

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
    /// <param name="origin">Optional container origin metadata for this queue slot.</param>
    public QueueItemViewModel(MediaViewModel media, QueueOrigin? origin = null)
    {
        ArgumentNullException.ThrowIfNull(media);
        Media = media;
        Origin = origin is { TrackItemsIndividually: false } ? origin : null;
    }

    public override string ToString() => Media.ToString();
}
