using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;

namespace Screenbox.Core.ViewModels;

/// <summary>
/// Represents a single-media entry in Recent Media wrapping a canonical <see cref="MediaViewModel"/>
/// with recent-card-specific active and playing state (active only when playing outside a container origin).
/// </summary>
public sealed partial class RecentMediaItemViewModel : ObservableRecipient,
    IRecentItem,
    IMediaContainer,
    IMediaPlaybackState,
    IRecipient<QueuePlaybackStateChangedMessage>
{
    /// <inheritdoc/>
    public MediaViewModel Media { get; }

    /// <inheritdoc/>
    public string Name => Media.Name;

    /// <inheritdoc cref="IMediaPlaybackState.IsMediaActive"/>
    [ObservableProperty]
    public partial bool IsMediaActive { get; set; }

    /// <inheritdoc cref="IMediaPlaybackState.IsPlaying"/>
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    public RecentMediaItemViewModel(MediaViewModel media, IMessenger? messenger = null)
        : base(messenger ?? WeakReferenceMessenger.Default)
    {
        ArgumentNullException.ThrowIfNull(media);
        Media = media;

        Messenger.Register<QueuePlaybackStateChangedMessage>(this);

        var request = new QueuePlaybackStateRequestMessage();
        Messenger.Send(request);
        if (request.HasReceivedResponse)
        {
            UpdatePlaybackState(request.Response);
        }
    }

    public void Receive(QueuePlaybackStateChangedMessage message)
    {
        UpdatePlaybackState(message.State);
    }

    private void UpdatePlaybackState(QueuePlaybackState state)
    {
        bool isActive = state.SessionId != Guid.Empty &&
                        state.CurrentItem is not null &&
                        (state.Origin is null || state.Origin.TrackItemsIndividually) &&
                        (ReferenceEquals(state.CurrentItem, Media) ||
                         string.Equals(
                             QueueOrigin.NormalizeLocation(state.CurrentItem.Location),
                             QueueOrigin.NormalizeLocation(Media.Location),
                             StringComparison.Ordinal));
        IsMediaActive = isActive;
        IsPlaying = isActive && state.IsPlaying;
    }

    public override string ToString() => Media.ToString();
}
