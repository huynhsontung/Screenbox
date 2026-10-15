using System;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Windows.Media;

namespace Screenbox.Core.Services;

/// <summary>
/// Bridge implementation of <see cref="ISettingsService"/> delegating to domain-specific settings contexts.
/// </summary>
[Obsolete("Use domain-specific settings contexts instead.")]
public sealed class SettingsService : ISettingsService
{
    private readonly SettingsContext _settings;

    public SettingsService() : this(CreateDefaultSettingsContext())
    {
    }

    public SettingsService(SettingsContext settings)
    {
        _settings = settings;
    }

    private static SettingsContext CreateDefaultSettingsContext()
    {
        var storage = new SettingsStorage();
        return new SettingsContext(
            new PlayerSettingsContext(storage),
            new LibrarySettingsContext(storage),
            new PlaybackSettingsContext(storage),
            new GeneralSettingsContext(storage),
            new AdvancedSettingsContext(storage),
            new PrivacySettingsContext(storage),
            new PersonalizationSettingsContext(storage));
    }

    public bool UseIndexer
    {
        get => _settings.Library.UseIndexer;
        set => _settings.Library.UseIndexer = value;
    }

    public ThemeOption Theme
    {
        get => _settings.Personalization.Theme;
        set => _settings.Personalization.Theme = value;
    }

    public PlayerAutoResizeOption PlayerAutoResize
    {
        get => _settings.Player.AutoResize;
        set => _settings.Player.AutoResize = value;
    }

    public int PersistentVolume
    {
        get => _settings.Playback.Volume;
        set => _settings.Playback.Volume = value;
    }

    public string PersistentSubtitleLanguage
    {
        get => _settings.Playback.SubtitleLanguage;
        set => _settings.Playback.SubtitleLanguage = value;
    }

    public int MaxVolume
    {
        get => _settings.Playback.MaxVolume;
        set => _settings.Playback.MaxVolume = value;
    }

    public bool ShowRecent
    {
        get => _settings.Privacy.ShowRecent;
        set => _settings.Privacy.ShowRecent = value;
    }

    public bool EnqueueAllFilesInFolder
    {
        get => _settings.General.EnqueueAllFilesInFolder;
        set => _settings.General.EnqueueAllFilesInFolder = value;
    }

    public string FrameCaptureFolderToken
    {
        get => _settings.Player.FrameCaptureFolderToken;
        set => _settings.Player.FrameCaptureFolderToken = value;
    }

    public bool RestorePlaybackPosition
    {
        get => _settings.Privacy.RestorePlaybackPosition;
        set => _settings.Privacy.RestorePlaybackPosition = value;
    }

    public bool PlayerShowControls
    {
        get => _settings.Player.ShowControls;
        set => _settings.Player.ShowControls = value;
    }

    public int PlayerControlsHideDelay
    {
        get => _settings.Player.ControlsHideDelay;
        set => _settings.Player.ControlsHideDelay = value;
    }

    public bool SearchRemovableStorage
    {
        get => _settings.Library.SearchRemovableStorage;
        set => _settings.Library.SearchRemovableStorage = value;
    }

    public MediaPlaybackAutoRepeatMode PersistentRepeatMode
    {
        get => _settings.Playback.RepeatMode;
        set => _settings.Playback.RepeatMode = value;
    }

    public string GlobalArguments
    {
        get => _settings.Advanced.GlobalArguments;
        set => _settings.Advanced.GlobalArguments = value;
    }

    public bool AdvancedMode
    {
        get => _settings.Advanced.AdvancedMode;
        set => _settings.Advanced.AdvancedMode = value;
    }

    public VideoUpscaleOption VideoUpscale
    {
        get => _settings.Advanced.VideoUpscale;
        set => _settings.Advanced.VideoUpscale = value;
    }

    public bool UseMultipleInstances
    {
        get => _settings.Advanced.UseMultipleInstances;
        set => _settings.Advanced.UseMultipleInstances = value;
    }

    public string LivelyActivePath
    {
        get => _settings.Player.LivelyActivePath;
        set => _settings.Player.LivelyActivePath = value;
    }

    public bool PersistentShowRemainingTime
    {
        get => _settings.Player.ShowRemainingTime;
        set => _settings.Player.ShowRemainingTime = value;
    }

    public bool PlayerShowChapters
    {
        get => _settings.Player.ShowChapters;
        set => _settings.Player.ShowChapters = value;
    }

    public bool PersistPlaybackPosition
    {
        get => _settings.Privacy.PersistPlaybackPosition;
        set => _settings.Privacy.PersistPlaybackPosition = value;
    }

    public int PlayerRewindStep
    {
        get => _settings.Player.RewindStep;
        set => _settings.Player.RewindStep = value;
    }

    public int PlayerFastForwardStep
    {
        get => _settings.Player.FastForwardStep;
        set => _settings.Player.FastForwardStep = value;
    }

    public PlaybackActionKind PlayerGestureTap
    {
        get => _settings.Player.GestureTap;
        set => _settings.Player.GestureTap = value;
    }

    public PlaybackActionKind PlayerGestureSwipeUp
    {
        get => _settings.Player.GestureSwipeUp;
        set => _settings.Player.GestureSwipeUp = value;
    }

    public PlaybackActionKind PlayerGestureSwipeDown
    {
        get => _settings.Player.GestureSwipeDown;
        set => _settings.Player.GestureSwipeDown = value;
    }

    public PlaybackActionKind PlayerGestureSwipeLeft
    {
        get => _settings.Player.GestureSwipeLeft;
        set => _settings.Player.GestureSwipeLeft = value;
    }

    public PlaybackActionKind PlayerGestureSwipeRight
    {
        get => _settings.Player.GestureSwipeRight;
        set => _settings.Player.GestureSwipeRight = value;
    }

    public bool PlayerGestureSlideVertical
    {
        get => _settings.Player.GestureSlideVertical;
        set => _settings.Player.GestureSlideVertical = value;
    }

    public bool PlayerGestureSlideHorizontal
    {
        get => _settings.Player.GestureSlideHorizontal;
        set => _settings.Player.GestureSlideHorizontal = value;
    }

    public bool PlayerGesturePressAndHold
    {
        get => _settings.Player.GesturePressAndHold;
        set => _settings.Player.GesturePressAndHold = value;
    }

    public SongSortOrder PersistentSongsSortOrder
    {
        get => _settings.Library.SongsSortOrder;
        set => _settings.Library.SongsSortOrder = value;
    }

    public AlbumSortOrder PersistentAlbumsSortOrder
    {
        get => _settings.Library.AlbumsSortOrder;
        set => _settings.Library.AlbumsSortOrder = value;
    }
}
