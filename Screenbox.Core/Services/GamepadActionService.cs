using System;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;

namespace Screenbox.Core.Services;

/// <summary>
/// Default implementation of <see cref="IGamepadActionService"/> that dispatches gamepad
/// actions to the rest of the app through messages.
/// </summary>
public sealed class GamepadActionService : IGamepadActionService
{
    /// <summary>
    /// Multiplier applied to the seek step for the "fast" seek actions.
    /// </summary>
    private const int FastSeekMultiplier = 4;

    private const int VolumeAdjustmentStep = 2;

    private readonly PlayerSettingsContext _playerSettings;

    public GamepadActionService(PlayerSettingsContext playerSettings)
    {
        _playerSettings = playerSettings;
    }

    public bool TryExecute(GamepadButtonAction action)
    {
        IMessenger messenger = WeakReferenceMessenger.Default;

        switch (action)
        {
            case GamepadButtonAction.PlayPause:
                messenger.Send(new TogglePlayPauseMessage(true));
                break;
            case GamepadButtonAction.ToggleFullscreen:
                messenger.Send(new ToggleFullscreenMessage());
                break;
            case GamepadButtonAction.TogglePlayerVisibility:
                messenger.Send(new TogglePlayerVisibilityMessage());
                break;
            case GamepadButtonAction.Rewind:
                Seek(messenger, -_playerSettings.RewindStep);
                break;
            case GamepadButtonAction.FastForward:
                Seek(messenger, _playerSettings.FastForwardStep);
                break;
            case GamepadButtonAction.RewindLong:
                Seek(messenger, -_playerSettings.RewindStep * FastSeekMultiplier);
                break;
            case GamepadButtonAction.FastForwardLong:
                Seek(messenger, _playerSettings.FastForwardStep * FastSeekMultiplier);
                break;
            case GamepadButtonAction.IncreaseVolume:
                ChangeVolume(messenger, VolumeAdjustmentStep);
                break;
            case GamepadButtonAction.DecreaseVolume:
                ChangeVolume(messenger, -VolumeAdjustmentStep);
                break;
            case GamepadButtonAction.PreviousTrack:
                messenger.Send(new TrackNavigationMessage(TrackNavigationDirection.Previous));
                break;
            case GamepadButtonAction.NextTrack:
                messenger.Send(new TrackNavigationMessage(TrackNavigationDirection.Next));
                break;
            case GamepadButtonAction.None:
            case GamepadButtonAction.OpenPlayQueue:
            case GamepadButtonAction.OpenSubtitleMenu:
            case GamepadButtonAction.OpenPlaybackOptions:
            default:
                // Player view actions and no-op actions are not handled here.
                return false;
        }

        return true;
    }

    private static void Seek(IMessenger messenger, int seconds)
    {
        messenger.SendSeekWithStatus(TimeSpan.FromSeconds(seconds));
    }

    private static void ChangeVolume(IMessenger messenger, int amount)
    {
        int volume = messenger.Send(new ChangeVolumeRequestMessage(amount, true));
        messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Volume, volume).ShowBadge().ShowMessage());
    }
}
