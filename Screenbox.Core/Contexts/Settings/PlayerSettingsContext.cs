using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for media player settings and gesture configuration.
/// </summary>
public sealed partial class PlayerSettingsContext : ObservableRecipient
{
    private const string AutoResizeKey = "Player/AutoResize";
    private const string ShowControlsKey = "Player/ShowControls";
    private const string ControlsHideDelayKey = "Player/ControlsHideDelay";
    private const string ShowChaptersKey = "Player/ShowChapters";
    private const string RewindStepKey = "Player/RewindStep";
    private const string FastForwardStepKey = "Player/FastForwardStep";
    private const string GestureTapKey = "Player/Gesture/Tap";
    private const string GestureSwipeUpKey = "Player/Gesture/SwipeUp";
    private const string GestureSwipeDownKey = "Player/Gesture/SwipeDown";
    private const string GestureSwipeLeftKey = "Player/Gesture/SwipeLeft";
    private const string GestureSwipeRightKey = "Player/Gesture/SwipeRight";
    private const string GestureSlideVerticalKey = "Player/Gesture/SlideVertical";
    private const string GestureSlideHorizontalKey = "Player/Gesture/SlideHorizontal";
    private const string GesturePressAndHoldKey = "Player/Gesture/PressAndHold";
    private const string LivelyPathKey = "Player/Lively/Path";

    private readonly ISettingsStorage _storage;
    private readonly bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlayerAutoResizeOption AutoResize { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowControls { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int ControlsHideDelay { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowChapters { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RewindStep { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int FastForwardStep { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackActionKind GestureTap { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackActionKind GestureSwipeUp { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackActionKind GestureSwipeDown { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackActionKind GestureSwipeLeft { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial PlaybackActionKind GestureSwipeRight { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool GestureSlideVertical { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool GestureSlideHorizontal { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool GesturePressAndHold { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LivelyActivePath { get; set; }

    public PlayerSettingsContext(ISettingsStorage storage)
    {
        _storage = storage;

        bool isXbox = SystemInformation.IsXbox;
        AutoResize = (PlayerAutoResizeOption)_storage.GetValue(AutoResizeKey, (int)PlayerAutoResizeOption.Never);
        ShowControls = _storage.GetValue(ShowControlsKey, true);
        ControlsHideDelay = _storage.GetValue(ControlsHideDelayKey, 3);
        ShowChapters = _storage.GetValue(ShowChaptersKey, true);
        RewindStep = _storage.GetValue(RewindStepKey, 5);
        FastForwardStep = _storage.GetValue(FastForwardStepKey, 5);

        int defaultTap = isXbox ? (int)PlaybackActionKind.None : (int)PlaybackActionKind.PlayPause;
        int defaultSwipeUp = isXbox ? (int)PlaybackActionKind.None : (int)PlaybackActionKind.IncreaseVolume;
        int defaultSwipeDown = isXbox ? (int)PlaybackActionKind.None : (int)PlaybackActionKind.DecreaseVolume;
        int defaultSwipeLeft = isXbox ? (int)PlaybackActionKind.None : (int)PlaybackActionKind.Rewind;
        int defaultSwipeRight = isXbox ? (int)PlaybackActionKind.None : (int)PlaybackActionKind.FastForward;

        GestureTap = (PlaybackActionKind)_storage.GetValue(GestureTapKey, defaultTap);
        GestureSwipeUp = (PlaybackActionKind)_storage.GetValue(GestureSwipeUpKey, defaultSwipeUp);
        GestureSwipeDown = (PlaybackActionKind)_storage.GetValue(GestureSwipeDownKey, defaultSwipeDown);
        GestureSwipeLeft = (PlaybackActionKind)_storage.GetValue(GestureSwipeLeftKey, defaultSwipeLeft);
        GestureSwipeRight = (PlaybackActionKind)_storage.GetValue(GestureSwipeRightKey, defaultSwipeRight);
        GestureSlideVertical = _storage.GetValue(GestureSlideVerticalKey, !isXbox);
        GestureSlideHorizontal = _storage.GetValue(GestureSlideHorizontalKey, !isXbox);
        GesturePressAndHold = _storage.GetValue(GesturePressAndHoldKey, !isXbox);
        LivelyActivePath = _storage.GetValue(LivelyPathKey, string.Empty);

        _isInitialized = true;
    }

    partial void OnAutoResizeChanged(PlayerAutoResizeOption value)
    {
        if (_isInitialized) _storage.SetValue(AutoResizeKey, (int)value);
    }

    partial void OnShowControlsChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(ShowControlsKey, value);
    }

    partial void OnControlsHideDelayChanged(int value)
    {
        if (_isInitialized) _storage.SetValue(ControlsHideDelayKey, value);
    }

    partial void OnShowChaptersChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(ShowChaptersKey, value);
    }

    partial void OnRewindStepChanged(int value)
    {
        if (_isInitialized) _storage.SetValue(RewindStepKey, value);
    }

    partial void OnFastForwardStepChanged(int value)
    {
        if (_isInitialized) _storage.SetValue(FastForwardStepKey, value);
    }

    partial void OnGestureTapChanged(PlaybackActionKind value)
    {
        if (_isInitialized) _storage.SetValue(GestureTapKey, (int)value);
    }

    partial void OnGestureSwipeUpChanged(PlaybackActionKind value)
    {
        if (_isInitialized) _storage.SetValue(GestureSwipeUpKey, (int)value);
    }

    partial void OnGestureSwipeDownChanged(PlaybackActionKind value)
    {
        if (_isInitialized) _storage.SetValue(GestureSwipeDownKey, (int)value);
    }

    partial void OnGestureSwipeLeftChanged(PlaybackActionKind value)
    {
        if (_isInitialized) _storage.SetValue(GestureSwipeLeftKey, (int)value);
    }

    partial void OnGestureSwipeRightChanged(PlaybackActionKind value)
    {
        if (_isInitialized) _storage.SetValue(GestureSwipeRightKey, (int)value);
    }

    partial void OnGestureSlideVerticalChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(GestureSlideVerticalKey, value);
    }

    partial void OnGestureSlideHorizontalChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(GestureSlideHorizontalKey, value);
    }

    partial void OnGesturePressAndHoldChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(GesturePressAndHoldKey, value);
    }

    partial void OnLivelyActivePathChanged(string value)
    {
        if (_isInitialized) _storage.SetValue(LivelyPathKey, value);
    }
}
