using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents an individual entry in a user playlist wrapping a canonical <see cref="MediaViewModel"/>.
/// </summary>
public sealed partial class PlaylistItemViewModel : ObservableObject, IMediaContainer
{
    /// <inheritdoc/>
    public MediaViewModel Media { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistItemViewModel"/> class for the specified media item.
    /// </summary>
    /// <param name="media">The canonical media view model represented by this playlist entry.</param>
    public PlaylistItemViewModel(MediaViewModel media)
    {
        ArgumentNullException.ThrowIfNull(media);
        Media = media;
    }

    public override string ToString() => Media.ToString();
}
