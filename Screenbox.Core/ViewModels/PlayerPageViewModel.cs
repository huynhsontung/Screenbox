#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayerPageViewModel : ObservableRecipient,
    IRecipient<UpdateStatusMessage>,
    IRecipient<UpdateVolumeStatusMessage>,
    IRecipient<TogglePlayerVisibilityMessage>,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<PlaylistCurrentItemChangedMessage>,
    IRecipient<ShowPlayPauseBadgeMessage>,
    IRecipient<OverrideControlsHideDelayMessage>,
    IRecipient<DragDropMessage>,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>,
    IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
{
    private const VirtualKey VK_OEM_COMMA = (VirtualKey)0xBC;
    private const VirtualKey VK_OEM_PERIOD = (VirtualKey)0xBE;

    [ObservableProperty] private bool _controlsHidden;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isPlayingBadge;
    [ObservableProperty] private bool _isOpening;
    [ObservableProperty] private bool _audioOnly;
    [ObservableProperty] private bool _showPlayPauseBadge;
    [ObservableProperty] private WindowViewMode _viewMode;
    [ObservableProperty] private NavigationViewDisplayMode _navigationViewDisplayMode;
    [ObservableProperty] private MediaViewModel? _media;
    [ObservableProperty] private bool _showVisualizer;
    [ObservableProperty] private bool _keyTipsVisible;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private PlayerVisibilityState _playerVisibility;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private MediaPlaybackState _playbackState;

    public bool SeekBarPointerInteracting { get; set; }

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _openingTimer;
    private readonly DispatcherQueueTimer _controlsVisibilityTimer;
    private readonly DispatcherQueueTimer _statusMessageTimer;
    private readonly DispatcherQueueTimer _playPauseBadgeTimer;
    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly IResourceService _resourceService;
    private readonly IFilesService _filesService;
    private readonly PlayerContext _playerContext;
    private bool _visibilityOverride;
    private bool _resizeNext;
    private DateTimeOffset _lastUpdated;

    public PlayerPageViewModel(IWindowService windowService, IResourceService resourceService,
        ISettingsService settingsService, IFilesService filesService, PlayerContext playerContext)
    {
        _windowService = windowService;
        _resourceService = resourceService;
        _settingsService = settingsService;
        _filesService = filesService;
        _playerContext = playerContext;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _openingTimer = _dispatcherQueue.CreateTimer();
        _controlsVisibilityTimer = _dispatcherQueue.CreateTimer();
        _statusMessageTimer = _dispatcherQueue.CreateTimer();
        _playPauseBadgeTimer = _dispatcherQueue.CreateTimer();
        _navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
        _playerVisibility = PlayerVisibilityState.Hidden;
        _lastUpdated = DateTimeOffset.MinValue;

        FocusManager.GotFocus += FocusManagerOnFocusChanged;
        _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackStateChanged += OnStateChanged;
            MediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        // Activate the view model's messenger
        IsActive = true;
    }

    public async void Receive(DragDropMessage message)
    {
        await OnDropAsync(message.Data);
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        if (message.NewValue == null) return;
        ShowVisualizer = AudioOnly && !string.IsNullOrEmpty(message.NewValue.Path);
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

    private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ViewMode = e.NewValue;
        });
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

    public void Receive(UpdateVolumeStatusMessage message)
    {
        Receive(new UpdateStatusMessage(
            _resourceService.GetString(ResourceName.VolumeChangeStatusMessage, message.Value)));
    }

    public void Receive(UpdateStatusMessage message)
    {
        // Don't show status message when player is not visible
        if (PlayerVisibility != PlayerVisibilityState.Visible && !string.IsNullOrEmpty(message.Value)) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            StatusMessage = message.Value;
            if (message.Value == null)
            {
                _statusMessageTimer.Stop();
                return;
            }

            _statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
        });
    }

    public async void Receive(PlaylistCurrentItemChangedMessage message)
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

    public void Receive(ShowPlayPauseBadgeMessage message)
    {
        IsPlayingBadge = message.IsPlaying;
        BlinkPlayPauseBadge();
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
        if (!ControlsHidden) return !_settingsService.PlayerTapGesture && TryHideControls(true);
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

    public void TogglePlayPause()
    {
        Messenger.Send(new TogglePlayPauseMessage(true));
    }

    /// <summary>
    /// Handles a volume increment or decrement based on the specified key.
    /// </summary>
    /// <remarks>
    /// <para>Volume change is only available when the player is visible.</para>
    /// The following keys determine the volume delta:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Add"/>, (<see cref="VirtualKey"/>)0xBB (VK_OEM_PLUS),
    /// or <see cref="VirtualKey.Up"/> (when player is visible): Increase volume by 5.</description></item>
    /// <item><description><see cref="VirtualKey.Subtract"/>, (<see cref="VirtualKey"/>)0xBD (VK_OEM_MINUS),
    /// or <see cref="VirtualKey.Down"/> (when player is visible): Decrease volume by 5.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <returns><see langword="true"/> if a volume change was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessChangeVolumeKeyDown(VirtualKey key)
    {
        if (MediaPlayer == null) return false;
        bool playerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
        int volumeChange;

        switch (key)
        {
            case (VirtualKey)0xBB:  // Plus ("+")
            case VirtualKey.Add:
            case VirtualKey.Up when playerVisible:
                volumeChange = 5;
                break;
            case (VirtualKey)0xBD:  // Minus ("-")
            case VirtualKey.Subtract:
            case VirtualKey.Down when playerVisible:
                volumeChange = -5;
                break;
            default:
                return false;
        }

        int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
        Messenger.Send(new UpdateVolumeStatusMessage(volume));
        return true;
    }

    /// <summary>
    /// Handles a seek operation based on keyboard input.
    /// </summary>
    /// <remarks>
    /// The following keys determine the seek direction:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Right"/> (when player is visible) or <see cref="VirtualKey.L"/>: Seek forward.</description></item>
    /// <item><description><see cref="VirtualKey.Left"/> (when player is visible) or <see cref="VirtualKey.J"/>: Seek backward.</description></item>
    /// </list>
    /// The seek duration is determined by the following modifier keys:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKeyModifiers.Control"/>: Seek by 10 seconds.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.Shift"/>: Seek by 1 second.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.None"/>: Seek by 5 seconds.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The modifier keys held during the key press.</param>
    /// <returns><see langword="true"/> if a seek operation was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessSeekKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (MediaPlayer == null) return false;
        bool playerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
        long seekAmount = 0;
        int direction;
        switch (key)
        {
            case VirtualKey.Left when playerVisible:
            case VirtualKey.J:
                direction = -1;
                break;
            case VirtualKey.Right when playerVisible:
            case VirtualKey.L:
                direction = 1;
                break;
            default:
                return false;
        }

        switch (modifiers)
        {
            case VirtualKeyModifiers.Control:
                seekAmount = 10000;
                break;
            case VirtualKeyModifiers.Shift:
                seekAmount = 1000;
                break;
            case VirtualKeyModifiers.None:
                seekAmount = 5000;
                break;
        }

        seekAmount *= direction;
        if (seekAmount != 0)
        {
            Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(seekAmount));
        }

        return true;
    }

    /// <summary>
    /// Handles a seek to position by percentage operation based on the specified key.
    /// </summary>
    /// <remarks>
    /// <para>Jumping to a specific position is only available when the player is visible.</para>
    /// The following keys determine the jump action:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKey.Home"/>: Seek to start.</description></item>
    /// <item><description><see cref="VirtualKey.End"/>: Seek to end.</description></item>
    /// <item><description><see cref="VirtualKey.NumberPad0"/> to <see cref="VirtualKey.NumberPad9"/>: Seek to percentage of duration.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <returns><see langword="true"/> if a seek to percentage was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessPercentJumpKeyDown(VirtualKey key)
    {
        if (MediaPlayer == null || PlayerVisibility != PlayerVisibilityState.Visible)
        {
            return false;
        }

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
                return false;
        }

        Messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
        return true;
    }

    /// <summary>
    /// Handles a playback rate increment or decrement based on keyboard input.
    /// </summary>
    /// <remarks>
    /// <para>Playback rate change is only available when the player is visible.</para>
    /// The following keys, in combination with the <see cref="VirtualKeyModifiers.Shift"/> modifier, determine the change:
    /// <list type="bullet">
    /// <item><description>(<see cref="VirtualKey"/>)0xBE (VK_OEM_PERIOD): Increase playback rate by 0.25x.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBC (VK_OEM_COMMA): Decrease playback rate by 0.25x.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The modifier keys held during the key press.</param>
    /// <returns><see langword="true"/> if a playback rate change was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessTogglePlaybackRateKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        const double PlaybackRateStep = 0.25;

        if (MediaPlayer == null ||
            modifiers != VirtualKeyModifiers.Shift ||
            PlayerVisibility != PlayerVisibilityState.Visible)
        {
            return false;
        }

        double rateDelta;
        switch (key)
        {
            case VK_OEM_PERIOD:  // Shift + . (">")
                rateDelta = PlaybackRateStep;
                break;
            case VK_OEM_COMMA:   // Shift + , ("<")
                rateDelta = -PlaybackRateStep;
                break;
            default:
                return false;
        }

        double newRate = Messenger.Send(new ChangePlaybackRateRequestMessage(rateDelta, true));
        Messenger.Send(new UpdateStatusMessage($"{newRate}×"));
        return true;
    }

    /// <summary>
    /// Handles frame stepping operation based on the specified key.
    /// </summary>
    /// <remarks>
    /// Frame stepping is only available when the player is visible, the media can be seeked, and playback is paused.
    /// <list type="bullet">
    /// <item><description>(<see cref="VirtualKey"/>)0xBE (VK_OEM_PERIOD): Step forward one frame.</description></item>
    /// <item><description>(<see cref="VirtualKey"/>)0xBC (VK_OEM_COMMA): Step backward one frame.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="key">The key that was pressed.</param>
    /// <returns><see langword="true"/> if a frame jump was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessFrameSteppingKeyDown(VirtualKey key)
    {
        if (PlayerVisibility != PlayerVisibilityState.Visible ||
            (!(MediaPlayer?.CanSeek ?? false)) ||
            MediaPlayer.PlaybackState != MediaPlaybackState.Paused)
        {
            return false;
        }

        switch (key)
        {
            case VK_OEM_PERIOD:
                MediaPlayer.StepForwardOneFrame();
                return true;
            case VK_OEM_COMMA:
                MediaPlayer.StepBackwardOneFrame();
                return true;
            default:
                return false;
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
    /// <returns><see langword="true"/> if a window resize was performed; otherwise, <see langword="false"/>.</returns>
    public bool ProcessResizeKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (MediaPlayer == null) return false;

        Size videoSize = new(MediaPlayer.NaturalVideoWidth, MediaPlayer.NaturalVideoHeight);
        var view = ApplicationView.GetForCurrentView();
        // Visible bounds always have 1 pixel less than actual window height?
        var currentSize = new Size(view.VisibleBounds.Width, view.VisibleBounds.Height + 1);
        // Desired step is 10% of the current window size
        // However, 10% step doesn't always give a round number for resizing and rounding error will accumulate
        // We want to maintain the original aspect ratio as long as possible
        var stepHeight = Math.Round(currentSize.Height * 0.1);
        var stepWidth = Math.Round(currentSize.Width * 0.1);
        var desiredStepSize = Math.Min(stepWidth / currentSize.Width, stepHeight / currentSize.Height);

        return key switch
        {
            VirtualKey.Number1 when modifiers == VirtualKeyModifiers.None => ResizeWindow(videoSize, 0.5),
            VirtualKey.Number2 when modifiers == VirtualKeyModifiers.None => ResizeWindow(videoSize, 1),
            VirtualKey.Number3 when modifiers == VirtualKeyModifiers.None => ResizeWindow(videoSize, 1.5),
            VirtualKey.Number4 when modifiers == VirtualKeyModifiers.None => ResizeWindow(videoSize, 0),
            (VirtualKey)0xBB when modifiers == VirtualKeyModifiers.Control => ResizeWindow(currentSize, 1 + desiredStepSize),   // Plus ("+")
            (VirtualKey)0xBD when modifiers == VirtualKeyModifiers.Control => ResizeWindow(currentSize, 1 - desiredStepSize),   // Minus ("-")
            _ => false,
        };
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

    partial void OnKeyTipsVisibleChanged(bool value)
    {
        if (value)
        {
            ControlsHidden = false;
        }
        else
        {
            DelayHideControls();
        }
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
                Playlist playlist = Messenger.Send(new PlaylistRequestMessage());
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

    private void BlinkPlayPauseBadge()
    {
        ShowPlayPauseBadge = true;
        _playPauseBadgeTimer.Debounce(() => ShowPlayPauseBadge = false, TimeSpan.FromMilliseconds(100));
    }

    public bool TryHideControls(bool skipFocusCheck = false)
    {
        bool shouldCheckPlaying = _settingsService.PlayerShowControls && !IsPlaying;
        if (PlayerVisibility != PlayerVisibilityState.Visible || shouldCheckPlaying ||
            SeekBarPointerInteracting || AudioOnly || ControlsHidden || KeyTipsVisible) return false;

        if (!skipFocusCheck)
        {
            Control? focused = FocusManager.GetFocusedElement() as Control;
            // Don't hide controls when a Slider is in focus since user can interact with Slider
            // using arrow keys without affecting focus.
            if (focused is Slider { IsFocusEngaged: true }) return false;

            // Don't hide controls when a flyout is in focus
            // Flyout is not in the same XAML tree of the Window content, use this fact to detect flyout opened
            Control? root = focused?.FindAscendant<Frame>(frame => frame == Window.Current.Content) ??
                            focused?.FindChild<Frame>(frame => frame == Window.Current.Content);
            if (root == null) return false;
        }

        ControlsHidden = true;

        // Workaround for PointerMoved is raised when show/hide cursor
        OverrideControlsDelayHide();

        return true;
    }

    private void DelayHideControls()
    {
        if (PlayerVisibility != PlayerVisibilityState.Visible || AudioOnly || KeyTipsVisible) return;

        int delayInSeconds = _settingsService.PlayerControlsHideDelay;
        _controlsVisibilityTimer.Debounce(() => TryHideControls(), TimeSpan.FromSeconds(delayInSeconds));
    }

    private void OverrideControlsDelayHide(int delay = 400)
    {
        _visibilityOverride = true;
        Task.Delay(delay).ContinueWith(_ => _visibilityOverride = false);
    }

    private void FocusManagerOnFocusChanged(object sender, FocusManagerGotFocusEventArgs e)
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
            if (ResizeWindow(desiredSize, 1)) return;

            // Resize to fill the screen only when video size is bigger than max window size
            Size maxWindowSize = _windowService.GetMaxWindowSize();
            if (sender.NaturalVideoWidth >= maxWindowSize.Width ||
                sender.NaturalVideoHeight >= maxWindowSize.Height)
                ResizeWindow(desiredSize, 0);
        });
    }

    private bool ResizeWindow(Size desiredSize, double scalar = 1)
    {
        if (scalar < 0 || _windowService.ViewMode != WindowViewMode.Default) return false;
        double actualScalar = _windowService.ResizeWindow(desiredSize, scalar);
        if (actualScalar > 0)
        {
            string status = _resourceService.GetString(ResourceName.ScaleStatus, $"{actualScalar * 100:0.##}%");
            Messenger.Send(new UpdateStatusMessage(status));
            return true;
        }

        return false;
    }
}
