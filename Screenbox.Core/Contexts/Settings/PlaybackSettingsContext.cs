using CommunityToolkit.Mvvm.ComponentModel;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
using Windows.Media;

namespace Screenbox.Core.Contexts;

/// <summary>
/// Observable state holder for playback preferences, audio levels, and library sorting.
/// </summary>
public sealed partial class PlaybackSettingsContext : ObservableRecipient
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

    private readonly ISettingsStorage _storage;
    private readonly bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Volume { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int MaxVolume { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial MediaPlaybackAutoRepeatMode RepeatMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string SubtitleLanguage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ShowRemainingTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool PersistPlaybackPosition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool RestorePlaybackPosition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial SongSortOrder SongsSortOrder { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AlbumSortOrder AlbumsSortOrder { get; set; }

    public PlaybackSettingsContext(ISettingsStorage storage)
    {
        _storage = storage;
        Volume = _storage.GetValue(PersistentVolumeKey, 100);
        MaxVolume = _storage.GetValue(MaxVolumeKey, 100);
        RepeatMode = (MediaPlaybackAutoRepeatMode)_storage.GetValue(PersistentRepeatModeKey, (int)MediaPlaybackAutoRepeatMode.None);
        SubtitleLanguage = _storage.GetValue(PersistentSubtitleLanguageKey, string.Empty);
        ShowRemainingTime = _storage.GetValue(PersistentShowRemainingTimeKey, false);
        PersistPlaybackPosition = _storage.GetValue(PersistPlaybackPositionKey, true);
        RestorePlaybackPosition = _storage.GetValue(RestorePlaybackPositionKey, false);
        SongsSortOrder = (SongSortOrder)_storage.GetValue(PersistentSongsSortOrderKey, (int)SongSortOrder.Title);
        AlbumsSortOrder = (AlbumSortOrder)_storage.GetValue(PersistentAlbumsSortOrderKey, (int)AlbumSortOrder.Title);
        _isInitialized = true;
        IsActive = true;
    }

    partial void OnVolumeChanged(int value)
    {
        if (_isInitialized) _storage.SetValue(PersistentVolumeKey, value);
    }

    partial void OnMaxVolumeChanged(int value)
    {
        if (_isInitialized) _storage.SetValue(MaxVolumeKey, value);
    }

    partial void OnRepeatModeChanged(MediaPlaybackAutoRepeatMode value)
    {
        if (_isInitialized) _storage.SetValue(PersistentRepeatModeKey, (int)value);
    }

    partial void OnSubtitleLanguageChanged(string value)
    {
        if (_isInitialized) _storage.SetValue(PersistentSubtitleLanguageKey, value);
    }

    partial void OnShowRemainingTimeChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(PersistentShowRemainingTimeKey, value);
    }

    partial void OnPersistPlaybackPositionChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(PersistPlaybackPositionKey, value);
    }

    partial void OnRestorePlaybackPositionChanged(bool value)
    {
        if (_isInitialized) _storage.SetValue(RestorePlaybackPositionKey, value);
    }

    partial void OnSongsSortOrderChanged(SongSortOrder value)
    {
        if (_isInitialized) _storage.SetValue(PersistentSongsSortOrderKey, (int)value);
    }

    partial void OnAlbumsSortOrderChanged(AlbumSortOrder value)
    {
        if (_isInitialized) _storage.SetValue(PersistentAlbumsSortOrderKey, (int)value);
    }
}
