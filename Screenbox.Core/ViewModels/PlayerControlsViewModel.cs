#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Events;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class PlayerControlsViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<IMediaPlayer?>>,
    IRecipient<SettingsChangedMessage>,
    IRecipient<TogglePlayPauseMessage>,
    IRecipient<ChangePlaybackRateRequestMessage>,
    IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
{
    public MediaListViewModel Playlist { get; }

    public bool ShouldBeAdaptive => !IsCompact && SystemInformation.IsDesktop;

    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isFullscreen;
    [ObservableProperty] private string? _titleName; // TODO: Handle VLC title name
    [ObservableProperty] private string? _chapterName;
    [ObservableProperty] private double _playbackRate;
    [ObservableProperty] private double _audioTimingOffset;
    [ObservableProperty] private double _subtitleTimingOffset;
    [ObservableProperty] private bool _isAdvancedModeActive;
    [ObservableProperty] private bool _isMinimal;
    [ObservableProperty] private bool _playerShowChapters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShouldBeAdaptive))]
    private bool _isCompact;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSnapshotCommand))]
    private bool _hasVideo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
    private bool _hasActiveItem;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IWindowService _windowService;
    private readonly ISettingsService _settingsService;
    private readonly PlayerContext _playerContext;
    private readonly CastContext _castContext;
    private readonly ICastService _castService;
    private Size _aspectRatio;

    public PlayerControlsViewModel(
        MediaListViewModel playlist,
        ISettingsService settingsService,
        IWindowService windowService,
        PlayerContext playerContext,
        CastContext castContext,
        ICastService castService)
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _windowService = windowService;
        _settingsService = settingsService;
        _playerContext = playerContext;
        _castContext = castContext;
        _castService = castService;
        _windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
        _playbackRate = 1.0;
        _audioTimingOffset = 0.0;
        _subtitleTimingOffset = 0.0;
        _isAdvancedModeActive = settingsService.AdvancedMode;
        _isMinimal = true;
        _playerShowChapters = settingsService.PlayerShowChapters;
        Playlist = playlist;
        Playlist.PropertyChanged += PlaylistViewModelOnPropertyChanged;

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            MediaPlayer.ChapterChanged += OnChapterChanged;
            MediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        // Keep IsPlaying in sync with Chromecast state while casting.
        _castContext.PropertyChanged += OnCastContextPropertyChanged;

        IsActive = true;
    }

    public void Receive(SettingsChangedMessage message)
    {
        switch (message.SettingsName)
        {
            case nameof(SettingsPageViewModel.AdvancedMode):
                IsAdvancedModeActive = _settingsService.AdvancedMode;
                break;
            case nameof(SettingsPageViewModel.PlayerShowChapters):
                PlayerShowChapters = _settingsService.PlayerShowChapters;
                break;
        }
    }

    public void Receive(PropertyChangedMessage<IMediaPlayer?> message)
    {
        if (message.Sender is not PlayerContext) return;
        if (message.OldValue is { } oldPlayer)
        {
            oldPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
            oldPlayer.ChapterChanged -= OnChapterChanged;
            oldPlayer.NaturalVideoSizeChanged -= OnNaturalVideoSizeChanged;
        }

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            MediaPlayer.ChapterChanged += OnChapterChanged;
            MediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }
    }

    public void Receive(TogglePlayPauseMessage message)
    {
        if (!HasActiveItem) return;
        if (message.ShowBadge)
        {
            PlayPauseWithBadge();
        }
        else
        {
            PlayPause();
        }
    }

    public void Receive(ChangePlaybackRateRequestMessage message)
    {
        SetPlaybackRate(message.Value);
        message.Reply(PlaybackRate);
    }

    public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
    {
        IsMinimal = message.NewValue != PlayerVisibilityState.Visible;
    }

    /// <summary>
    /// Toggles the playback state of the active item and displays a badge indicating the new state.
    /// </summary>
    public void PlayPauseWithBadge()
    {
        if (!HasActiveItem) return;
        Messenger.Send(new ShowPlayPauseBadgeMessage(!IsPlaying));
        PlayPause();
    }

    /// <summary>
    /// Handles toggling the subtitle track during media playback based on keyboard input.
    /// </summary>
    /// <remarks>
    /// The following modifiers determine the toggle action:
    /// <list type="bullet">
    /// <item><description><see cref="VirtualKeyModifiers.None"/> toggles the only subtitle track on or off.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.Control"/> cycles forward through the available subtitle tracks.</description></item>
    /// <item><description><see cref="VirtualKeyModifiers.Control"/> + <see cref="VirtualKeyModifiers.Shift"/> cycles backward through the available subtitle tracks.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="modifiers">The modifier keys held during the key press.</param>
    /// <returns>
    /// A tuple where <c>Handled</c> is <see langword="true"/> if the toggle succeeded, and
    /// <c>TrackLabel</c> is the label of the newly selected track, or <see langword="null"/> if subtitles were disabled.
    /// </returns>
    public (bool Handled, string? TrackLabel) ProcessToggleSubtitleKeyDown(VirtualKeyModifiers modifiers)
    {
        if (MediaPlayer?.PlaybackItem is null)
        {
            return (false, null);
        }

        PlaybackSubtitleTrackList subtitleTracks = MediaPlayer.PlaybackItem.SubtitleTracks;
        if (subtitleTracks.Count == 0)
        {
            return (false, null);
        }

        switch (modifiers)
        {
            case VirtualKeyModifiers.None when subtitleTracks.Count == 1:
                if (subtitleTracks.SelectedIndex >= 0)
                {
                    subtitleTracks.SelectedIndex = -1;
                }
                else
                {
                    subtitleTracks.SelectedIndex = 0;
                }

                break;
            case VirtualKeyModifiers.Control:
                if (subtitleTracks.SelectedIndex == subtitleTracks.Count - 1)
                {
                    subtitleTracks.SelectedIndex = -1;
                }
                else
                {
                    subtitleTracks.SelectedIndex++;
                }

                break;
            case VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift:
                if (subtitleTracks.SelectedIndex == -1)
                {
                    subtitleTracks.SelectedIndex = subtitleTracks.Count - 1;
                }
                else
                {
                    subtitleTracks.SelectedIndex--;
                }

                break;
            default:
                return (false, null);
        }

        string? label = subtitleTracks.SelectedIndex == -1
            ? null
            : subtitleTracks[subtitleTracks.SelectedIndex].Label;

        return (true, label);
    }

    /// <summary>
    /// Sends a status message via the messenger.
    /// The view layer should call this after formatting a localized status string.
    /// </summary>
    /// <param name="message">The formatted status message to display.</param>
    public void SendStatusMessage(string? message)
    {
        Messenger.Send(new UpdateStatusMessage(message));
    }

    partial void OnPlaybackRateChanged(double value)
    {
        if (MediaPlayer == null) return;
        MediaPlayer.PlaybackRate = value;
    }

    partial void OnAudioTimingOffsetChanged(double value)
    {
        if (MediaPlayer == null) return;

        if (MediaPlayer is VlcMediaPlayer vlcMediaPlayer)
        {
            vlcMediaPlayer.AudioDelay = value;
        }
    }

    partial void OnSubtitleTimingOffsetChanged(double value)
    {
        if (MediaPlayer == null) return;

        if (MediaPlayer is VlcMediaPlayer vlcMediaPlayer)
        {
            vlcMediaPlayer.SubtitleDelay = value;
        }
    }

    private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MediaListViewModel.CurrentItem):
                HasActiveItem = Playlist.CurrentItem != null;
                SubtitleTimingOffset = 0;
                AudioTimingOffset = 0;
                break;
        }
    }

    private void OnNaturalVideoSizeChanged(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() => HasVideo = MediaPlayer?.NaturalVideoHeight > 0);
    }

    private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // Do not overwrite cast-driven state while a session is active.
            if (_castContext.IsCasting) return;
            IsPlaying = sender.PlaybackState is MediaPlaybackState.Playing or MediaPlaybackState.Opening;
        });
    }

    private void OnChapterChanged(IMediaPlayer sender, object? args)
    {
        _dispatcherQueue.TryEnqueue(() => ChapterName = sender.Chapter?.Title);
    }

    /// <summary>
    /// Keeps <see cref="IsPlaying"/> in sync with the Chromecast device state while casting,
    /// and restores it from the local player state once the session ends.
    /// </summary>
    private void OnCastContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CastContext.CastIsPlaying) when _castContext.IsCasting:
                IsPlaying = _castContext.CastIsPlaying;
                break;
            case nameof(CastContext.IsCasting) when !_castContext.IsCasting:
                // Casting ended — restore IsPlaying from local player state.
                IsPlaying = MediaPlayer?.PlaybackState is MediaPlaybackState.Playing or MediaPlaybackState.Opening;
                break;
        }
    }

    private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
    {
        switch (e.NewValue)
        {
            case WindowViewMode.Default:
                IsFullscreen = false;
                IsCompact = false;
                break;
            case WindowViewMode.Compact:
                IsCompact = true;
                IsFullscreen = false;
                break;
            case WindowViewMode.FullScreen:
                IsFullscreen = true;
                IsCompact = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    private void ResetMediaPlayback()
    {
        if (MediaPlayer == null) return;
        TimeSpan pos = MediaPlayer.Position;
        MediaViewModel? item = Playlist.CurrentItem;
        Playlist.CurrentItem = null;
        Playlist.CurrentItem = item;
        _dispatcherQueue.TryEnqueue(() =>
        {
            MediaPlayer.Play();
            MediaPlayer.Position = pos;
        });
    }

    [RelayCommand]
    private void SetPlaybackRate(double rate)
    {
        PlaybackRate = rate;
    }

    [RelayCommand]
    private void SetAspectRatio(string aspect)
    {
        switch (aspect)
        {
            case "Fit":
                _aspectRatio = new Size(0, 0);
                break;
            case "Fill":
                _aspectRatio = new Size(double.NaN, double.NaN);
                break;
            default:
                string[] values = aspect.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (values.Length != 2) return;
                if (!double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double width)) return;
                if (!double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double height)) return;
                _aspectRatio = new Size(width, height);
                break;
        }

        Messenger.Send(new ChangeAspectRatioMessage(_aspectRatio));
    }

    [RelayCommand]
    private async Task ToggleCompactLayoutAsync()
    {
        if (IsCompact)
        {
            await _windowService.TryExitCompactLayoutAsync();
        }
        else if (MediaPlayer?.NaturalVideoHeight > 0)
        {
            double aspectRatio = MediaPlayer.NaturalVideoWidth / (double)MediaPlayer.NaturalVideoHeight;
            await _windowService.TryEnterCompactLayoutAsync(new Size(240 * aspectRatio, 240));
        }
        else
        {
            await _windowService.TryEnterCompactLayoutAsync(new Size(240, 240));
        }
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        if (IsCompact) return;
        if (IsFullscreen)
        {
            _windowService.ExitFullScreen();
        }
        else
        {
            _windowService.TryEnterFullScreen();
        }
    }

    [RelayCommand(CanExecute = nameof(HasActiveItem))]
    private void PlayPause()
    {
        // While casting, route play/pause to the Chromecast device instead of the local player.
        // The local player remains paused for the duration of the cast session.
        if (_castContext.IsCasting && _castContext.Client is { } castClient)
        {
            if (_castContext.CastIsPlaying)
            {
                _ = _castService.PauseAsync(castClient);
            }
            else
            {
                _ = _castService.PlayAsync(castClient);
            }

            return;
        }

        if (IsPlaying)
        {
            MediaPlayer?.Pause();
        }
        else
        {
            MediaPlayer?.Play();
        }
    }

    /// <summary>
    /// Saves a snapshot of the current video frame to the Pictures library.
    /// Sends a <see cref="FailedToSaveFrameNotificationMessage"/> on failure.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasVideo))]
    private async Task SaveSnapshotAsync()
    {
        if (MediaPlayer?.PlaybackState is not (MediaPlaybackState.Paused or MediaPlaybackState.Playing)) return;
        try
        {
            StorageFile file = await SaveSnapshotInternalAsync(MediaPlayer);
            Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
        }
        catch (UnauthorizedAccessException)
        {
            Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Pictures));
        }
        catch (Exception e)
        {
            Messenger.Send(new FailedToSaveFrameNotificationMessage(e.Message));
        }
    }

    private static async Task<StorageFile> SaveSnapshotInternalAsync(IMediaPlayer mediaPlayer)
    {
        if (mediaPlayer is not VlcMediaPlayer player)
        {
            throw new NotImplementedException("Not supported on non VLC players");
        }

        StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
            $"snapshot_{DateTimeOffset.Now.Ticks}",
            CreationCollisionOption.FailIfExists);

        try
        {
            if (!player.VlcPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                throw new Exception("VLC failed to save snapshot");

            StorageFile file = (await tempFolder.GetFilesAsync())[0];
            StorageLibrary pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            StorageFolder defaultSaveFolder = pictureLibrary.SaveFolder;
            StorageFolder destFolder =
                await defaultSaveFolder.CreateFolderAsync("Screenbox",
                    CreationCollisionOption.OpenIfExists);
            return await file.CopyAsync(destFolder, $"Screenbox_{DateTimeOffset.Now:yyyyMMdd_HHmmss}{file.FileType}",
                NameCollisionOption.GenerateUniqueName);
        }
        finally
        {
            await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }
    }
}
