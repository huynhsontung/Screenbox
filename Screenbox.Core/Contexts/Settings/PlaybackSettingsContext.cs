using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Windows.Media;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for playback preferences, audio levels, and library sorting.
/// </summary>
public sealed partial class PlaybackSettingsContext : SettingsContextBase
{
    private const string PersistentVolumeKey = "Values/Volume";
    private const string MaxVolumeKey = "Values/MaxVolume";
    private const string PersistentRepeatModeKey = "Values/RepeatMode";
    private const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";
    private const string PersistentShowRemainingTimeKey = "Values/ShowRemainingTime";
    private const string PersistPlaybackPositionKey = "Privacy/PersistPlaybackPosition";
    private const string RestorePlaybackPositionKey = "General/RestorePlaybackPosition";
    private const string PersistentSongsSortOrderKey = "Values/Songs/SortOrder";
    private const string PersistentAlbumsSortOrderKey = "Values/Albums/SortOrder";

    public int Volume
    {
        get => Get(PersistentVolumeKey, 100);
        set => Set(PersistentVolumeKey, value);
    }

    public int MaxVolume
    {
        get => Get(MaxVolumeKey, 100);
        set => Set(MaxVolumeKey, value);
    }

    public MediaPlaybackAutoRepeatMode RepeatMode
    {
        get => Get(PersistentRepeatModeKey, MediaPlaybackAutoRepeatMode.None);
        set => Set(PersistentRepeatModeKey, value);
    }

    public string SubtitleLanguage
    {
        get => Get(PersistentSubtitleLanguageKey, string.Empty);
        set => Set(PersistentSubtitleLanguageKey, value);
    }

    public bool ShowRemainingTime
    {
        get => Get(PersistentShowRemainingTimeKey, false);
        set => Set(PersistentShowRemainingTimeKey, value);
    }

    public bool PersistPlaybackPosition
    {
        get => Get(PersistPlaybackPositionKey, true);
        set => Set(PersistPlaybackPositionKey, value);
    }

    public bool RestorePlaybackPosition
    {
        get => Get(RestorePlaybackPositionKey, false);
        set => Set(RestorePlaybackPositionKey, value);
    }

    public SongSortOrder SongsSortOrder
    {
        get => Get(PersistentSongsSortOrderKey, SongSortOrder.Title);
        set => Set(PersistentSongsSortOrderKey, value);
    }

    public AlbumSortOrder AlbumsSortOrder
    {
        get => Get(PersistentAlbumsSortOrderKey, AlbumSortOrder.Title);
        set => Set(PersistentAlbumsSortOrderKey, value);
    }

    public PlaybackSettingsContext(ISettingsStorage storage) : base(storage)
    {
    }
}
