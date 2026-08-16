using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayerPageViewModel : ObservableRecipient,
    IRecipient<TogglePlayerVisibilityMessage>,
    IRecipient<PlayerOsdUpdateMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<QueueCurrentItemChangedMessage>,
    IRecipient<OverrideControlsHideDelayMessage>,
    IRecipient<DragDropMessage>,
    IRecipient<VisualizerChangedMessage>,
    IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>,
    IRecipient<PropertyChangedMessage<WindowViewMode>>
{
    private const VirtualKey VK_OEM_PLUS = (VirtualKey)0xBB;
    private const VirtualKey VK_OEM_COMMA = (VirtualKey)0xBC;
    private const VirtualKey VK_OEM_MINUS = (VirtualKey)0xBD;
    private const VirtualKey VK_OEM_PERIOD = (VirtualKey)0xBE;

    [ObservableProperty] public partial bool ControlsHidden { get; set; }
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial bool IsOpening { get; set; }
    [ObservableProperty] public partial bool AudioOnly { get; set; }
    [ObservableProperty] public partial WindowViewMode ViewMode { get; set; }
    [ObservableProperty] public partial NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }
    [ObservableProperty] public partial MediaViewModel? Media { get; set; }
    [ObservableProperty] public partial bool ShowVisualizer { get; set; }

    [ObservableProperty]
    public partial PlaybackCommandKind CurrentPlaybackCommand { get; set; }

    [ObservableProperty]
    public partial bool IsOsdMessageVisible { get; set; }

    [ObservableProperty]
    public partial bool IsOsdBadgeVisible { get; set; }

    [ObservableProperty]
    public partial object? OsdMessageValue { get; set; }

    /// <summary>
    /// Set to <see langword="true"/> by the view model to signal the view to close the play queue flyout.
    /// The view should reset this to <see langword="false"/> after closing the flyout.
    /// </summary>
    [ObservableProperty] public partial bool ShouldClosePlayQueueFlyout { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsPlayerVisibilityVisible))]
    public partial PlayerVisibilityState PlayerVisibility { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MediaPlaybackState PlaybackState { get; set; }

    public bool SeekBarPointerInteracting { get; set; }

    public bool IsPlayerVisibilityVisible => PlayerVisibility is PlayerVisibilityState.Visible;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _openingTimer;
    private readonly DispatcherQueueTimer _controlsVisibilityTimer;
    private readonly DispatcherQueueTimer _statusMessageTimer;
    private readonly DispatcherQueueTimer _playPauseBadgeTimer;
    private readonly DispatcherQueueTimer _playPauseHoldTimer;
    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly IFilesService _filesService;
    private readonly PlayerContext _playerContext;
    private bool _visibilityOverride;
    private bool _resizeNext;
    private bool _isPlayPauseHoldActive;
    private double _playbackRateBeforeHold;

    public PlayerPageViewModel(IWindowService windowService,
        ISettingsService settingsService, IFilesService filesService, PlayerContext playerContext,
        INavigationService navigationService)
    {
        _windowService = windowService;
        _settingsService = settingsService;
        _filesService = filesService;
        _playerContext = playerContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _openingTimer = _dispatcherQueue.CreateTimer();
        _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
        _statusMessageTimer = _dispatcherQueue.CreateTimer();
        _playPauseBadgeTimer = _dispatcherQueue.CreateTimer();
        _playPauseHoldTimer = _dispatcherQueue.CreateTimer();
        NavigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
        PlayerVisibility = PlayerVisibilityState.Hidden;

        // Strong reference handlers. No need to unsubscribe since PlayerPageViewModel has the same lifetime as the app.
        FocusManager.GotFocus += FocusManagerOnFocusChanged;
        navigationService.Navigated += OnNavigationServiceNavigated;

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackStateChanged += OnStateChanged;
            MediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        Messenger.Register<TogglePlayerVisibilityMessage>(this);
        Messenger.Register<PlayerOsdUpdateMessage>(this);
        Messenger.Register<PropertyChangedMessage<IMediaPlayer?>>(this);
        Messenger.Register<QueueCurrentItemChangedMessage>(this);
        Messenger.Register<OverrideControlsHideDelayMessage>(this);
        Messenger.Register<DragDropMessage>(this);
        Messenger.Register<VisualizerChangedMessage>(this);
        Messenger.Register<PropertyChangedMessage<NavigationViewDisplayMode>>(this);
        Messenger.Register<PropertyChangedMessage<WindowViewMode>>(this);
    }

    public async void Receive(DragDropMessage message)
    {
        await OnDropAsync(message.Data);
    }

    public void Receive(VisualizerChangedMessage message)
    {
        if (message.Path is null) return;
        ShowVisualizer = AudioOnly && !string.IsNullOrEmpty(message.Path);
    }

    public void Receive(TogglePlayerVisibilityMessage message)
    {
        switch (PlayerVisibility)
        {
            case PlayerVisibilityState.Visible:
                GoBack();
                break;
            case PlayerVisibilityState.Minimal:
                RestorePlayer();
                break;
        }
    }

    public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
    {
        NavigationViewDisplayMode = message.NewValue;
    }

    public void Receive(PropertyChangedMessage<WindowViewMode> message)
    {
        if (message.Sender is not WindowContext) return;
        _dispatcherQueue.TryEnqueue(() => ViewMode = message.NewValue);
    }

    private void OnNavigationServiceNavigated(object? sender, EventArgs e)
    {
        if (PlayerVisibility != PlayerVisibilityState.Visible) return;
        GoBack();
        ShouldClosePlayQueueFlyout = true;
    }

    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;

        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.PlaybackStateChanged -= OnStateChanged;
            oldPlayer.NaturalVideoSizeChanged -= OnNaturalVideoSizeChanged;
        }

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackStateChanged += OnStateChanged;
            MediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }
    }

    public void Receive(PlayerOsdUpdateMessage message)
    {
        CurrentPlaybackCommand = message.Kind;
        OsdMessageValue = message.Value;

        // Don't show status message when player is not visible.
        if (message.HasMessage && PlayerVisibility == PlayerVisibilityState.Visible)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsOsdMessageVisible = true;

                if (message.Duration == Timeout.InfiniteTimeSpan)
                    return;

                _statusMessageTimer.Debounce(() =>
                {
                    IsOsdMessageVisible = false;
                }, message.Duration ?? TimeSpan.FromMilliseconds(1000));
            });
        }

        if (message.HasBadge)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsOsdBadgeVisible = true;

                if (message.Duration == Timeout.InfiniteTimeSpan)
                    return;

                _playPauseBadgeTimer.Debounce(() =>
                {
                    IsOsdBadgeVisible = false;
                }, message.Duration ?? TimeSpan.FromMilliseconds(1000));
            });
        }
    }

    public async void Receive(QueueCurrentItemChangedMessage message)
    {
        MediaViewModel? current = message.Value;
        _dispatcherQueue.TryEnqueue(() => UpdatePropertiesWithCurrentItem(current));
        if (current != null)
        {
            await current.LoadDetailsAsync(_filesService);
            await current.LoadThumbnailAsync();

            // Process again in case media type changed after loading details
            _dispatcherQueue.TryEnqueue(() => UpdatePropertiesWithCurrentItem(current));
        }
    }

    public void Receive(OverrideControlsHideDelayMessage message)
    {
        OverrideControlsDelayHide(message.Delay);
    }

    public async Task OnDropAsync(DataPackageView data)
    {
        try
        {
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem>? items = await data.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    if (items.Count == 1 && items[0] is StorageFile file && file.IsSupportedSubtitle() &&
                        MediaPlayer is VlcMediaPlayer player && Media?.Item.Value != null)
                    {
                        Media.Item.Value.SubtitleTracks.AddExternalSubtitle(player, file, true);
                        Messenger.Send(new SubtitleAddedNotificationMessage(file));
                    }
                    else
                    {
                        Messenger.Send(new PlayFilesMessage(items));
                    }

                    return;
                }
            }

            if (data.Contains(StandardDataFormats.WebLink))
            {
                Uri? uri = await data.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    Messenger.Send(new PlayMediaMessage(uri));
                }
            }
        }
        catch (Exception exception)
        {
            Messenger.Send(new MediaLoadFailedNotificationMessage(exception.Message, string.Empty));
        }
    }

    public bool OnPlayerClick()
    {
        if (!ControlsHidden) return (_settingsService.PlayerGestureTap == PlaybackActionKind.None) && TryHideControls(true);
        ControlsHidden = false;
        DelayHideControls();
        return true;
    }

    public void OnPointerMoved()
    {
        if (_visibilityOverride) return;
        ControlsHidden = false;

        if (SeekBarPointerInteracting) return;
        DelayHideControls();
    }

    /// <summary>
    /// Handles the play/pause key press interaction, initiating the hold
    /// behavior that temporarily increases playback speed.
    /// </summary>
    /// <remarks>
    /// If the media is currently <see cref="MediaPlaybackState.Paused"/> and
    /// <see cref="ISettingsService.PlayerGesturePressAndHold"/> is not enabled,
    /// the hold behavior is not available. To activate the hold behavior, the play/pause
    /// key needs to be pressed and held down for at least 500 milliseconds.
    /// </remarks>
    public void HandlePlayPauseKeyDown()
    {
        const double HoldingSpeed = 2.0;

        if (!_settingsService.PlayerGesturePressAndHold ||
            _isPlayPauseHoldActive ||
            MediaPlayer is null || MediaPlayer.PlaybackState is MediaPlaybackState.Paused)
            return;

        _playPauseHoldTimer.Debounce(() =>
        {
            _playbackRateBeforeHold = MediaPlayer.PlaybackRate;
            // If the rate is already faster than the holding speed, set it to twice the holding speed.
            double effectiveHoldingSpeed = MediaPlayer.PlaybackRate >= HoldingSpeed ? HoldingSpeed * 2.0 : HoldingSpeed;
            if (MediaPlayer.PlaybackRate != effectiveHoldingSpeed)
            {
                Messenger.Send(new ChangePlaybackRateRequestMessage(effectiveHoldingSpeed));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateUp, Value: effectiveHoldingSpeed, Duration: Timeout.InfiniteTimeSpan).WithMessage());
            }

            _isPlayPauseHoldActive = true;
        }, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Handles the release of the play/pause key to either toggle playback or
    /// revert to the original playback rate.
    /// </summary>
    /// <remarks>
    /// If the play/pause key is pressed and released quickly, toggles playback state;
    /// if held, restores the original playback rate.
    /// </remarks>
    public void HandlePlayPauseKeyUp()
    {
        _playPauseHoldTimer.Stop();

        if (!_isPlayPauseHoldActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(true));
        }
        else
        {
            if (MediaPlayer is not null && MediaPlayer.PlaybackRate != _playbackRateBeforeHold)
            {
                Messenger.Send(new ChangePlaybackRateRequestMessage(_playbackRateBeforeHold));
                Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.RateDown, Value: _playbackRateBeforeHold).WithMessage());
            }

            _isPlayPauseHoldActive = false;
        }
    }

    /// <summary>
    /// Handles a volume increment or decrement based on the specified key.
    /// </summary>
    /// <remarks>
    /// The following keys determine the volume delta:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Add"/>, (<see cref="VirtualKey"/>)0xBB (VK_OEM_PLUS),
    /// or <see cref="VirtualKey.Up"/> (when player is visible): Increase volume by <c>5</c>.</description></item>
    /// <item><description><see cref="VirtualKey.Subtract"/>, (<see cref="VirtualKey"/>)0xBD (VK_OEM_MINUS),
    /// or <see cref="VirtualKey.Down"/> (when player is visible): Decrease volume by <c>5</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    public void HandleVolumeKey(VirtualKey key)
    {
        if (MediaPlayer is null)
            return;

        bool isPlayerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
        int delta = key switch
        {
            VK_OEM_PLUS or VirtualKey.Add => 5,
            VK_OEM_MINUS or VirtualKey.Subtract => -5,
            VirtualKey.Up when isPlayerVisible => 5,
            VirtualKey.Down when isPlayerVisible => -5,
            _ => 0,
        };

        if (delta == 0)
            return;

        int newValue = Messenger.Send(new ChangeVolumeRequestMessage(delta, true));
        Messenger.Send(
            new PlayerOsdUpdateMessage(
                delta > 0 ? PlaybackCommandKind.VolumeUp : PlaybackCommandKind.VolumeDown,
                Value: newValue)
            .WithBadge()
            .WithMessage());
    }

    /// <summary>
    /// Handles a seek operation based on keyboard input.
    /// </summary>
    /// <remarks>
    /// The following keys determine the seek direction:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.L"/> or <see cref="VirtualKey.Right"/> (when player is visible): Seek forward.</description></item>
    /// <item><description><see cref="VirtualKey.J"/> or <see cref="VirtualKey.Left"/> (when player is visible): Seek backward.</description></item>
    /// </list>
    /// The seek duration is determined by the following modifier keys:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKeyModifiers.None"/>: Seek using the default interval.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.Control"/>: Seek using double (<c>2×</c>) the configured interval.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.Shift"/>: Seek using one-fifth (<c>1/5</c>) of the configured interval.</description></item>
    /// </list>
    /// The default seek intervals are based on the configured <see cref="ISettingsService.PlayerRewindStep"/>
    /// and <see cref="ISettingsService.PlayerFastForwardStep"/> values.
    /// </remarks>
    /// <param name="key">A value of the enumeration that specifies the key that was pressed.</param>
    /// <param name="modifiers">A bitwise combination of the enumeration values that specifies the modifier keys held during the key press.</param>
    public void HandleSeekKey(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (MediaPlayer is null)
            return;

        bool isPlayerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
        int step = key switch
        {
            VirtualKey.Left when isPlayerVisible => -_settingsService.PlayerRewindStep,
            VirtualKey.Right when isPlayerVisible => _settingsService.PlayerFastForwardStep,
            VirtualKey.J => -_settingsService.PlayerRewindStep,
            VirtualKey.L => _settingsService.PlayerFastForwardStep,
            _ => 0,
        };

        double factor = modifiers switch
        {
            VirtualKeyModifiers.None => 1,
            VirtualKeyModifiers.Control => 2,
            VirtualKeyModifiers.Shift => 0.2,
            _ => 0,
        };

        double delta = step * factor;
        if (delta == 0)
            return;

        PositionChangedResult result = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.FromSeconds(delta), isOffset: true, debounce: false));
        TimeSpan seekOffset = result.NewPosition - result.OriginalPosition;
        string seekExtra = $"{(seekOffset > TimeSpan.Zero ? '+' : string.Empty)}{Humanizer.ToDuration(seekOffset)}";
        string message = $"{Humanizer.ToDuration(result.NewPosition)} / {Humanizer.ToDuration(result.NaturalDuration)} ({seekExtra})";
        Messenger.Send(
            new PlayerOsdUpdateMessage(
                delta > 0 ? PlaybackCommandKind.FastForward : PlaybackCommandKind.Rewind,
                Value: message)
            .WithBadge()
            .WithMessage());
    }

    /// <summary>
    /// Handles jumping to a specific playback position by percentage based on the specified key.
    /// </summary>
    /// <remarks>
    /// <para>Requires <see cref="PlayerVisibility"/> to be <see cref="PlayerVisibilityState.Visible"/>.</para>
    /// The following keys determine the jump action:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Home"/>: Seek to start.</description></item>
    /// <item><description><see cref="VirtualKey.End"/>: Seek to end.</description></item>
    /// <item><description><see cref="VirtualKey.NumberPad0"/> to <see cref="VirtualKey.NumberPad9"/>: Seek to percentage of duration.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    public void HandlePercentJumpKey(VirtualKey key)
    {
        if (MediaPlayer is null || PlayerVisibility != PlayerVisibilityState.Visible)
            return;

        PositionChangedResult result;
        string extra = string.Empty;
        switch (key)
        {
            case VirtualKey.Home:
                result = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.Zero));
                break;
            case VirtualKey.End:
                result = Messenger.Send(new ChangeTimeRequestMessage(MediaPlayer.NaturalDuration));
                break;
            case VirtualKey.NumberPad0:
            case VirtualKey.NumberPad1:
            case VirtualKey.NumberPad2:
            case VirtualKey.NumberPad3:
            case VirtualKey.NumberPad4:
            case VirtualKey.NumberPad5:
            case VirtualKey.NumberPad6:
            case VirtualKey.NumberPad7:
            case VirtualKey.NumberPad8:
            case VirtualKey.NumberPad9:
                int percent = (key - VirtualKey.NumberPad0) * 10;
                TimeSpan newPosition = MediaPlayer.NaturalDuration * (0.01 * percent);
                result = Messenger.Send(new ChangeTimeRequestMessage(newPosition));
                extra = $"{percent}%";
                break;
            default:
                return;
        }

        Messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
    }

    /// <summary>
    /// Handles a playback rate increment or decrement based on keyboard input.
    /// </summary>
    /// <remarks>
    /// <para>Requires <see cref="PlayerVisibility"/> to be <see cref="PlayerVisibilityState.Visible"/>.</para>
    /// The following keys, in combination with the <see cref="VirtualKeyModifiers.Shift"/> modifier, determine the change:
    /// <list type="bullet">
    /// <item><description>(<see cref="VirtualKey"/>)0xBE (VK_OEM_PERIOD): Increase playback rate by 0.25x.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBC (VK_OEM_COMMA): Decrease playback rate by 0.25x.</description></item>
    /// </list>
    /// The playback rate is clamped between 0.25x and 4x.
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The modifier keys held during the key press.</param>
    public void HandlePlaybackRateToggleKey(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        const double PlaybackRateStep = 0.25;

        if (MediaPlayer is null ||
            modifiers != VirtualKeyModifiers.Shift ||
            PlayerVisibility != PlayerVisibilityState.Visible)
            return;

        double delta = key switch
        {
            VK_OEM_PERIOD => PlaybackRateStep,  // Shift + . (">")
            VK_OEM_COMMA => -PlaybackRateStep,  // Shift + , ("<")
            _ => 0.0,
        };

        if (delta == 0.0)
            return;

        double newRate = Messenger.Send(new ChangePlaybackRateRequestMessage(Math.Clamp(MediaPlayer.PlaybackRate + delta, 0.25, 4)));
        Messenger.Send(
            new PlayerOsdUpdateMessage(
                delta > 0 ? PlaybackCommandKind.RateUp : PlaybackCommandKind.RateDown,
                Value: newRate)
            .WithBadge()
            .WithMessage());
    }

    /// <summary>
    /// Handles frame-stepping based on the specified key.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PlayerVisibility"/> to be <see cref="PlayerVisibilityState.Visible"/>,
    /// <see cref="MediaPlayer.CanSeek"/> to be true, and <see cref="MediaPlayer.PlaybackState"/>
    /// to be <see cref="MediaPlaybackState.Paused"/>.
    /// <list type="bullet">
    /// <item><description>(<see cref="VirtualKey"/>)0xBE (VK_OEM_PERIOD): Step forward one frame.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBC (VK_OEM_COMMA): Step backward one frame.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    public void HandleFrameSteppingKey(VirtualKey key)
    {
        if (PlayerVisibility != PlayerVisibilityState.Visible ||
            !(MediaPlayer?.CanSeek ?? false) ||
            MediaPlayer.PlaybackState != MediaPlaybackState.Paused)
            return;

        switch (key)
        {
            case VK_OEM_PERIOD:
                MediaPlayer.StepForwardOneFrame();
                return;
            case VK_OEM_COMMA:
                MediaPlayer.StepBackwardOneFrame();
                return;
        }
    }

    /// <summary>
    /// Handles a window resize operation based on keyboard input.
    /// </summary>
    /// <remarks>
    /// The following keys determine the resize action:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Number1"/>: Resize to 50% of video size.</description></item>
    /// <item><description><see cref="VirtualKey.Number2"/>: Resize to 100% of video size.</description></item>
    /// <item><description><see cref="VirtualKey.Number3"/>: Resize to 150% of video size.</description></item>
    /// <item><description><see cref="VirtualKey.Number4"/>: Resize to fill screen.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBB (VK_OEM_PLUS) with <see cref="VirtualKeyModifiers.Control"/>: Increase window size by 10%.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBD (VK_OEM_MINUS) with <see cref="VirtualKeyModifiers.Control"/>: Decrease window size by 10%.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The modifier keys held during the key press.</param>
    /// <param name="currentSize">The size of the current window.</param>
    public void HandleResizeKey(VirtualKey key, VirtualKeyModifiers modifiers, Size currentSize)
    {
        if (MediaPlayer is null)
            return;

        var videoSize = new Size(MediaPlayer.NaturalVideoWidth, MediaPlayer.NaturalVideoHeight);

        // Desired step is 10% of the current window size
        // However, 10% step doesn't always give a round number for resizing and rounding error will accumulate
        // We want to maintain the original aspect ratio as long as possible
        double stepHeight = Math.Round(currentSize.Height * 0.1);
        double stepWidth = Math.Round(currentSize.Width * 0.1);
        double desiredStep = Math.Min(stepWidth / currentSize.Width, stepHeight / currentSize.Height);

        double? scale = key switch
        {
            VirtualKey.Number1 when modifiers == VirtualKeyModifiers.None => 0.5,
            VirtualKey.Number2 when modifiers == VirtualKeyModifiers.None => 1.0,
            VirtualKey.Number3 when modifiers == VirtualKeyModifiers.None => 1.5,
            VirtualKey.Number4 when modifiers == VirtualKeyModifiers.None => 0.0,
            VK_OEM_PLUS when modifiers == VirtualKeyModifiers.Control => 1 + desiredStep,   // Plus  ("+")
            VK_OEM_MINUS when modifiers == VirtualKeyModifiers.Control => 1 - desiredStep,  // Minus ("-")
            _ => null,
        };

        if (scale is null)
            return;

        double? newScalar = ResizeWindow(desiredSize: modifiers == VirtualKeyModifiers.None ? videoSize : currentSize, scalar: scale.Value);
        if (newScalar is null or <= 0)
            return;

        Messenger.Send(new PlayerOsdUpdateMessage(PlaybackCommandKind.Scale, Value: scale.Value).WithMessage());
    }

    public void OnFileLaunched()
    {
        if (_settingsService.PlayerAutoResize == PlayerAutoResizeOption.OnLaunch)
            _resizeNext = true;
    }

    // Hidden button acts as a focus sink when controls are hidden
    public void HiddenButtonOnClick()
    {
        ControlsHidden = false;
        if (SystemInformation.IsDesktop)
        {
            // On Desktop, user expect Space to pause without needing to see the controls
            Messenger.Send(new TogglePlayPauseMessage(true));
        }
    }

    partial void OnControlsHiddenChanged(bool value)
    {
        if (value)
        {
            _windowService.HideCursor();
        }
        else
        {
            _windowService.ShowCursor();
        }

        Messenger.Send(new PlayerControlsVisibilityChangedMessage(!value));
    }

    partial void OnPlayerVisibilityChanged(PlayerVisibilityState value)
    {
        if (value != PlayerVisibilityState.Visible) ControlsHidden = false;
    }

    [RelayCommand]
    public void GoBack()
    {
        // Only allow back when not in fullscreen or compact overlay
        // Doing so would break layout logic
        switch (_windowService.ViewMode)
        {
            case WindowViewMode.FullScreen:
                _windowService.ExitFullScreen();
                break;
            case WindowViewMode.Compact:
                _windowService.TryExitCompactLayoutAsync();
                break;
            case WindowViewMode.Default:
                Playlist playlist = Messenger.Send(new QueueRequestMessage());
                bool hasItemsInQueue = playlist.Items.Count > 0;
                PlayerVisibility = hasItemsInQueue ? PlayerVisibilityState.Minimal : PlayerVisibilityState.Hidden;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void RestorePlayer()
    {
        PlayerVisibility = PlayerVisibilityState.Visible;
    }

    public bool TryHideControls(bool skipFocusCheck = false)
    {
        bool shouldCheckPlaying = _settingsService.PlayerShowControls && !IsPlaying;
        if (PlayerVisibility != PlayerVisibilityState.Visible || shouldCheckPlaying ||
            SeekBarPointerInteracting || AudioOnly || ControlsHidden) return false;

        if (!skipFocusCheck)
        {
            Control? focused = FocusManager.GetFocusedElement() as Control;
            // Don't hide controls when a Slider is in focus since user can interact with Slider
            // using arrow keys without affecting focus.
            if (focused is Slider { IsFocusEngaged: true }) return false;

            // Do not hide controls while a popup is open.
            bool isPopupOpen = VisualTreeHelper.GetOpenPopups(Window.Current).Any();
            if (isPopupOpen) return false;
        }

        ControlsHidden = true;

        // Workaround for PointerMoved is raised when show/hide cursor
        OverrideControlsDelayHide();

        return true;
    }

    private void DelayHideControls()
    {
        if (PlayerVisibility != PlayerVisibilityState.Visible || AudioOnly) return;

        int delayInSeconds = _settingsService.PlayerControlsHideDelay;
        _controlsVisibilityTimer.Debounce(() => TryHideControls(), TimeSpan.FromSeconds(delayInSeconds));
    }

    private void OverrideControlsDelayHide(int delay = 400)
    {
        _visibilityOverride = true;
        Task.Delay(delay).ContinueWith(_ => _visibilityOverride = false);
    }

    private void FocusManagerOnFocusChanged(object? sender, FocusManagerGotFocusEventArgs e)
    {
        if (_visibilityOverride) return;
        ControlsHidden = false;
        DelayHideControls();
    }

    private void UpdatePropertiesWithCurrentItem(MediaViewModel? current)
    {
        Media = current;
        AudioOnly = current == null || current.MediaType == MediaPlaybackType.Music;
        ShowVisualizer = current != null && AudioOnly && !string.IsNullOrEmpty(_settingsService.LivelyActivePath);
        if (current != null)
        {
            // Auto-resize player window
            bool shouldBeVisible = _settingsService.PlayerAutoResize == PlayerAutoResizeOption.Always && !AudioOnly;
            if (PlayerVisibility != PlayerVisibilityState.Visible)
            {
                PlayerVisibility = shouldBeVisible ? PlayerVisibilityState.Visible : PlayerVisibilityState.Minimal;
            }

            if (AudioOnly)
            {
                // If it's audio only, don't resize on next video playback
                _resizeNext = false;
            }
        }
        else if (PlayerVisibility == PlayerVisibilityState.Minimal)
        {
            PlayerVisibility = PlayerVisibilityState.Hidden;
        }
    }

    private void OnStateChanged(IMediaPlayer sender, object? args)
    {
        _openingTimer.Stop();
        MediaPlaybackState state = sender.PlaybackState;
        if (state == MediaPlaybackState.Opening)
        {
            _openingTimer.Debounce(() => IsOpening = state == MediaPlaybackState.Opening, TimeSpan.FromSeconds(0.5));
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            PlaybackState = state;
            IsPlaying = state == MediaPlaybackState.Playing;
            IsOpening = false;

            if (!IsPlaying && _settingsService.PlayerShowControls)
            {
                ControlsHidden = false;
            }

            if (!IsPlaying && !_settingsService.PlayerShowControls)
            {
                DelayHideControls();
            }

            if (!ControlsHidden && IsPlaying)
            {
                DelayHideControls();
            }
        });
    }

    private void OnNaturalVideoSizeChanged(IMediaPlayer sender, EventArgs args)
    {
        if (!_resizeNext && _settingsService.PlayerAutoResize != PlayerAutoResizeOption.Always) return;
        _resizeNext = false;

        _dispatcherQueue.TryEnqueue(() =>
        {
            Size desiredSize = new(sender.NaturalVideoWidth, sender.NaturalVideoHeight);
            if (ResizeWindow(desiredSize, 1).HasValue) return;

            // Resize to fill the screen only when video size is bigger than max window size
            Size maxWindowSize = _windowService.GetMaxWindowSize();
            if (sender.NaturalVideoWidth >= maxWindowSize.Width ||
                sender.NaturalVideoHeight >= maxWindowSize.Height)
                ResizeWindow(desiredSize, 0);
        });
    }

    private double? ResizeWindow(Size desiredSize, double scalar = 1)
    {
        if (scalar < 0 || _windowService.ViewMode != WindowViewMode.Default) return null;
        double actualScalar = _windowService.ResizeWindow(desiredSize, scalar);
        return actualScalar > 0 ? actualScalar : null;
    }
}
