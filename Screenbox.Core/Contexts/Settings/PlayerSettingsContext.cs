using System;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media player UI presentation, transport controls, seek intervals,
/// touch and pointer gestures, frame capture location, Lively audio visualizer, video upscaling,
/// and LibVLC engine configuration.
/// </summary>
public sealed partial class PlayerSettingsContext : SettingsContextBase<PlayerSettingsContext>
{
    private const string AutoResizeKey = "Player/AutoResize";
    private const string ShowControlsKey = "Player/ShowControls";
    private const string ControlsHideDelayKey = "Player/ControlsHideDelay";
    private const string ShowChaptersKey = "Player/ShowChapters";
    private const string PersistentShowRemainingTimeKey = "Values/ShowRemainingTime";
    private const string RewindStepKey = "Player/RewindStep";
    private const string FastForwardStepKey = "Player/FastForwardStep";
    private const string GeneralFrameCaptureFolderTokenKey = "General/FrameCaptureFolderToken";
    private const string GestureTapKey = "Player/Gesture/Tap";
    private const string GestureSwipeUpKey = "Player/Gesture/SwipeUp";
    private const string GestureSwipeDownKey = "Player/Gesture/SwipeDown";
    private const string GestureSwipeLeftKey = "Player/Gesture/SwipeLeft";
    private const string GestureSwipeRightKey = "Player/Gesture/SwipeRight";
    private const string GestureSlideVerticalKey = "Player/Gesture/SlideVertical";
    private const string GestureSlideHorizontalKey = "Player/Gesture/SlideHorizontal";
    private const string GesturePressAndHoldKey = "Player/Gesture/PressAndHold";
    private const string LivelyPathKey = "Player/Lively/Path";
    private const string AdvancedModeKey = "Advanced/IsEnabled";
    private const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
    private const string GlobalArgumentsKey = "Values/GlobalArguments";
    private const string GamepadXActionKey = "Player/Gamepad/XAction";
    private const string GamepadYActionKey = "Player/Gamepad/YAction";
    private const string GamepadViewActionKey = "Player/Gamepad/ViewAction";
    private const string GamepadMenuActionKey = "Player/Gamepad/MenuAction";
    private const string GamepadLeftShoulderActionKey = "Player/Gamepad/LeftShoulderAction";
    private const string GamepadRightShoulderActionKey = "Player/Gamepad/RightShoulderAction";
    private const string GamepadLeftTriggerActionKey = "Player/Gamepad/LeftTriggerAction";
    private const string GamepadRightTriggerActionKey = "Player/Gamepad/RightTriggerAction";
    private const string GamepadLeftThumbstickButtonActionKey = "Player/Gamepad/LeftThumbstickButtonAction";
    private const string GamepadRightThumbstickButtonActionKey = "Player/Gamepad/RightThumbstickButtonAction";

    private static bool IsXbox => SystemInformation.IsXbox;

    public PlayerAutoResizeOption AutoResize
    {
        get => Get(AutoResizeKey, PlayerAutoResizeOption.Never);
        set => Set(AutoResizeKey, value);
    }

    public bool ShowControls
    {
        get => Get(ShowControlsKey, true);
        set => Set(ShowControlsKey, value);
    }

    public int ControlsHideDelay
    {
        get => Get(ControlsHideDelayKey, 3);
        set => Set(ControlsHideDelayKey, value);
    }

    public bool ShowChapters
    {
        get => Get(ShowChaptersKey, true);
        set => Set(ShowChaptersKey, value);
    }

    public bool ShowRemainingTime
    {
        get => Get(PersistentShowRemainingTimeKey, false);
        set => Set(PersistentShowRemainingTimeKey, value);
    }

    public int RewindStep
    {
        get => Get(RewindStepKey, 5);
        set => Set(RewindStepKey, value);
    }

    public int FastForwardStep
    {
        get => Get(FastForwardStepKey, 5);
        set => Set(FastForwardStepKey, value);
    }

    public string FrameCaptureFolderToken
    {
        get => Get(GeneralFrameCaptureFolderTokenKey, string.Empty);
        set => Set(GeneralFrameCaptureFolderTokenKey, value);
    }

    public PlaybackActionKind GestureTap
    {
        get => Get(GestureTapKey, IsXbox ? PlaybackActionKind.None : PlaybackActionKind.PlayPause);
        set => Set(GestureTapKey, value);
    }

    public PlaybackActionKind GestureSwipeUp
    {
        get => Get(GestureSwipeUpKey, IsXbox ? PlaybackActionKind.None : PlaybackActionKind.IncreaseVolume);
        set => Set(GestureSwipeUpKey, value);
    }

    public PlaybackActionKind GestureSwipeDown
    {
        get => Get(GestureSwipeDownKey, IsXbox ? PlaybackActionKind.None : PlaybackActionKind.DecreaseVolume);
        set => Set(GestureSwipeDownKey, value);
    }

    public PlaybackActionKind GestureSwipeLeft
    {
        get => Get(GestureSwipeLeftKey, IsXbox ? PlaybackActionKind.None : PlaybackActionKind.Rewind);
        set => Set(GestureSwipeLeftKey, value);
    }

    public PlaybackActionKind GestureSwipeRight
    {
        get => Get(GestureSwipeRightKey, IsXbox ? PlaybackActionKind.None : PlaybackActionKind.FastForward);
        set => Set(GestureSwipeRightKey, value);
    }

    public bool GestureSlideVertical
    {
        get => Get(GestureSlideVerticalKey, !IsXbox);
        set => Set(GestureSlideVerticalKey, value);
    }

    public bool GestureSlideHorizontal
    {
        get => Get(GestureSlideHorizontalKey, !IsXbox);
        set => Set(GestureSlideHorizontalKey, value);
    }

    public bool GesturePressAndHold
    {
        get => Get(GesturePressAndHoldKey, !IsXbox);
        set => Set(GesturePressAndHoldKey, value);
    }

    public string LivelyActivePath
    {
        get => Get(LivelyPathKey, string.Empty);
        set => Set(LivelyPathKey, value);
    }

    public bool AdvancedMode
    {
        get => Get(AdvancedModeKey, false);
        set => Set(AdvancedModeKey, value);
    }

    public VideoUpscaleOption VideoUpscale
    {
        get => Get(AdvancedVideoUpscaleKey, VideoUpscaleOption.Linear);
        set => Set(AdvancedVideoUpscaleKey, value);
    }

    public string GlobalArguments
    {
        get => Get(GlobalArgumentsKey, string.Empty);
        set => Set(GlobalArgumentsKey, value);
    }

    public GamepadButtonAction GamepadXAction
    {
        get => Get(GamepadXActionKey, GamepadButtonAction.PlayPause);
        set => Set(GamepadXActionKey, value);
    }

    public GamepadButtonAction GamepadYAction
    {
        get => Get(GamepadYActionKey, GamepadButtonAction.ToggleFullscreen);
        set => Set(GamepadYActionKey, value);
    }

    public GamepadButtonAction GamepadViewAction
    {
        get => Get(GamepadViewActionKey, GamepadButtonAction.TogglePlayerVisibility);
        set => Set(GamepadViewActionKey, value);
    }

    public GamepadButtonAction GamepadMenuAction
    {
        get => Get(GamepadMenuActionKey, GamepadButtonAction.OpenPlaybackOptions);
        set => Set(GamepadMenuActionKey, value);
    }

    public GamepadButtonAction GamepadLeftShoulderAction
    {
        get => Get(GamepadLeftShoulderActionKey, GamepadButtonAction.Rewind);
        set => Set(GamepadLeftShoulderActionKey, value);
    }

    public GamepadButtonAction GamepadRightShoulderAction
    {
        get => Get(GamepadRightShoulderActionKey, GamepadButtonAction.FastForward);
        set => Set(GamepadRightShoulderActionKey, value);
    }

    public GamepadButtonAction GamepadLeftTriggerAction
    {
        get => Get(GamepadLeftTriggerActionKey, GamepadButtonAction.RewindLong);
        set => Set(GamepadLeftTriggerActionKey, value);
    }

    public GamepadButtonAction GamepadRightTriggerAction
    {
        get => Get(GamepadRightTriggerActionKey, GamepadButtonAction.FastForwardLong);
        set => Set(GamepadRightTriggerActionKey, value);
    }

    public GamepadButtonAction GamepadLeftThumbstickButtonAction
    {
        get => Get(GamepadLeftThumbstickButtonActionKey, GamepadButtonAction.OpenSubtitleMenu);
        set => Set(GamepadLeftThumbstickButtonActionKey, value);
    }

    public GamepadButtonAction GamepadRightThumbstickButtonAction
    {
        get => Get(GamepadRightThumbstickButtonActionKey, GamepadButtonAction.None);
        set => Set(GamepadRightThumbstickButtonActionKey, value);
    }

    public PlayerSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
