using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.MediaSources;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class CompositeTrackPickerViewModel : ObservableRecipient,
    IRecipient<QueueCurrentItemChangedMessage>
{
    public ObservableCollection<string> SubtitleTracks { get; }

    public ObservableCollection<string> AudioTracks { get; }

    public ObservableCollection<string> VideoTracks { get; }

    private ISubtitleTrackList? ItemSubtitleTrackList => MediaPlayer?.PlaybackItem?.SubtitleTracks;

    private ISingleSelectMediaTrackList<AudioTrack>? ItemAudioTrackList => MediaPlayer?.PlaybackItem?.AudioTracks;

    private ISingleSelectMediaTrackList<VideoTrack>? ItemVideoTrackList => MediaPlayer?.PlaybackItem?.VideoTracks;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    /// <summary>
    /// The currently selected subtitle track UI index.
    /// <list type="bullet">
    /// <item><description><c>0</c> = subtitles disabled (corresponds to the prepended "Disable" option in the UI).</description></item>
    /// <item><description><c>1</c> to <c>SubtitleTracks.Count</c> = the <c>SelectedIndex</c> of an enabled subtitle track in the UI; the
    /// actual underlying subtitle track index is typically obtained by subtracting <c>1</c> from this value.</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty] public partial int SubtitleTrackIndex { get; set; }

    /// <summary>
    /// The currently selected audio track index. <c>-1</c> means no track is selected.
    /// </summary>
    [ObservableProperty] public partial int AudioTrackIndex { get; set; }

    /// <summary>
    /// The currently selected video track index. <c>-1</c> means no track is selected.
    /// </summary>
    [ObservableProperty] public partial int VideoTrackIndex { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the flyout shows audio, subtitle,
    /// or composite options in the picker.
    /// </summary>
    /// <returns>
    /// A value of the enumeration that indicates whether the flyout shows audio, subtitle,
    /// or composite options. The default is <b><see cref="TrackPickerDisplayMode.Composite"/></b>.
    /// </returns>
    [ObservableProperty]
    public partial TrackPickerDisplayMode DisplayMode { get; set; } = TrackPickerDisplayMode.Composite;

    private readonly IFilesService _filesService;
    private readonly ISubtitleService _subtitleService;
    private readonly PlaybackSettingsContext _playbackSettings;
    private readonly PlayerContext _playerContext;
    private bool _flyoutOpened;
    private CancellationTokenSource? _cts;

    public CompositeTrackPickerViewModel(
        PlayerContext playerContext,
        IFilesService filesService,
        ISubtitleService subtitleService,
        PlaybackSettingsContext playbackSettings)
    {
        _filesService = filesService;
        _subtitleService = subtitleService;
        _playbackSettings = playbackSettings;
        _playerContext = playerContext;
        SubtitleTracks = new ObservableCollection<string>();
        AudioTracks = new ObservableCollection<string>();
        VideoTracks = new ObservableCollection<string>();
        DisplayMode = TrackPickerDisplayMode.Composite;

        Messenger.Register<QueueCurrentItemChangedMessage>(this);
    }

    /// <summary>
    /// Try load a subtitle in the same directory with the same name
    /// </summary>
    public async void Receive(QueueCurrentItemChangedMessage message)
    {
        _cts?.Cancel();
        if (MediaPlayer is null) return;
        if (message.Value is not { MediaType: MediaPlaybackType.Video } media ||
            media.Source is not IFileMediaSource fileSource ||
            await _filesService.ResolveFileAsync(fileSource) is not { } file)
            return;

        bool subtitleInitialized = false;
        var playbackSubtitleTrackList = MediaPlayer.PlaybackItem?.SubtitleTracks;
        if (playbackSubtitleTrackList == null) return;
        if (playbackSubtitleTrackList.Count > 0) subtitleInitialized = true;

        if (!subtitleInitialized)
        {
            IReadOnlyList<StorageFile> subtitles = await _subtitleService.FindSubtitlesForFileAsync(file, message.NeighboringFilesQuery);
            foreach (StorageFile subtitleFile in subtitles)
            {
                // Preload subtitle but don't select it
                playbackSubtitleTrackList.AddExternalSubtitle(subtitleFile, false);
            }
        }

        if (!subtitleInitialized && MediaPlayer.PlaybackItem is { } playbackItem)
        {
            try
            {
                using var cts = new CancellationTokenSource();
                _cts = cts;
                await playbackItem.WaitForParsedAsync(TimeSpan.FromSeconds(5), cts.Token);
            }
            catch (OperationCanceledException)
            {
                // pass
            }
            finally
            {
                _cts = null;
            }
        }

        int preferredIndex = _subtitleService.FindPreferredSubtitleIndex(playbackSubtitleTrackList, _playbackSettings.SubtitleLanguage);
        if (preferredIndex >= 0)
        {
            playbackSubtitleTrackList.SelectedIndex = preferredIndex;
        }
    }

    partial void OnSubtitleTrackIndexChanged(int value)
    {
        if (ItemSubtitleTrackList == null) return;

        // VM index 0 maps to actual track index -1, which is "Disable"
        // Decrement value by 1 to convert from display index to actual subtitle track index
        value = Math.Max(-1, value - 1);
        if (value >= ItemSubtitleTrackList.Count) return;
        ItemSubtitleTrackList.SelectedIndex = value;

        if (!_flyoutOpened) return;

        if (value < 0)
        {
            _playbackSettings.SubtitleLanguage = string.Empty;
        }
        else if (value < SubtitleTracks.Count)
        {
            var subtitle = ItemSubtitleTrackList[value];
            _playbackSettings.SubtitleLanguage =
                $"{subtitle.LanguageTag},{subtitle.Language},{LanguageHelper.GetPreferredLanguage().Substring(0, 2)}";
        }
    }

    partial void OnAudioTrackIndexChanged(int value)
    {
        if (ItemAudioTrackList != null && value >= 0 && value < ItemAudioTrackList.Count)
            ItemAudioTrackList.SelectedIndex = value;
    }

    partial void OnVideoTrackIndexChanged(int value)
    {
        if (ItemVideoTrackList != null && value >= 0 && value < ItemVideoTrackList.Count)
            ItemVideoTrackList.SelectedIndex = value;
    }

    /// <summary>
    /// Adds a subtitle file to the current media.
    /// </summary>
    [RelayCommand]
    private async Task AddSubtitleAsync()
    {
        try
        {
            if (ItemSubtitleTrackList == null) return;
            StorageFile? file = await _filesService.PickFileAsync([.. FilesHelpers.SupportedSubtitleFormats, "*"]);
            if (file == null) return;

            ItemSubtitleTrackList.AddExternalSubtitle(file, true);
            Messenger.Send(new NotificationMessage(NotificationLevel.Success, NotificationKind.SubtitleAdded, message: file.Name));
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.SubtitleLoadFailed, message: e.Message));
        }
    }

    public void OnFlyoutOpening()
    {
        UpdateSubtitleTrackList();
        UpdateAudioTrackList();
        UpdateVideoTrackList();
        SubtitleTrackIndex = (ItemSubtitleTrackList?.SelectedIndex + 1) ?? 0;
        AudioTrackIndex = ItemAudioTrackList?.SelectedIndex ?? -1;
        VideoTrackIndex = ItemVideoTrackList?.SelectedIndex ?? -1;

        _flyoutOpened = true;
    }

    public void OnFlyoutClosed()
    {
        _flyoutOpened = false;
        DisplayMode = TrackPickerDisplayMode.Composite;
    }

    [RelayCommand]
    private void SetDisplayMode(TrackPickerDisplayMode value)
    {
        DisplayMode = value;
    }

    private void UpdateAudioTrackList()
    {
        if (ItemAudioTrackList == null) return;
        ItemAudioTrackList.Refresh();
        var trackLabels = ItemAudioTrackList.Select(track => track.Label).ToList();
        AudioTracks.SyncItems(trackLabels);
    }

    private void UpdateVideoTrackList()
    {
        if (ItemVideoTrackList == null) return;
        ItemVideoTrackList.Refresh();
        var trackLabels = ItemVideoTrackList.Select(track => track.Label).ToList();
        VideoTracks.SyncItems(trackLabels);
    }

    private void UpdateSubtitleTrackList()
    {
        if (ItemSubtitleTrackList == null) return;
        var trackLabels = ItemSubtitleTrackList.Select(track => track.Label).ToList();
        SubtitleTracks.SyncItems(trackLabels);
    }
}
