using System;
using System.Linq;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media player UI presentation, transport controls, seek intervals,
/// touch and pointer gestures, frame capture location, Lively audio visualizer, video upscaling,
/// and LibVLC engine configuration.
/// </summary>
public sealed partial class PlayerSettingsContext : SettingsContextBase
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
        set
        {
            string sanitized = SanitizeArguments(value);
            if (!Set(GlobalArgumentsKey, sanitized) && sanitized != value)
            {
                OnPropertyChanged();
            }
        }
    }

    public PlayerSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }

    private static string SanitizeArguments(string raw)
    {
        string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith('-') && s != "--").ToArray();
        return string.Join(' ', args);
    }
}
