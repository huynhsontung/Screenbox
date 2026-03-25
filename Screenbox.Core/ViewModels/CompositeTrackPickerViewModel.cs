#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Playback;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.Storage.Search;

namespace Screenbox.Core.ViewModels;

public sealed partial class CompositeTrackPickerViewModel : ObservableRecipient,
    IRecipient<PlaylistCurrentItemChangedMessage>
{
    public ObservableCollection<string> SubtitleTracks { get; }

    public ObservableCollection<string> AudioTracks { get; }

    public ObservableCollection<string> VideoTracks { get; }

    private PlaybackSubtitleTrackList? ItemSubtitleTrackList => MediaPlayer?.PlaybackItem?.SubtitleTracks;

    private PlaybackAudioTrackList? ItemAudioTrackList => MediaPlayer?.PlaybackItem?.AudioTracks;

    private PlaybackVideoTrackList? ItemVideoTrackList => MediaPlayer?.PlaybackItem?.VideoTracks;

    private IMediaPlayer? MediaPlayer => _playerContext.MediaPlayer;

    /// <summary>
    /// The currently selected subtitle track UI index.
    /// <list type="bullet">
    /// <item><description><c>0</c> = subtitles disabled (corresponds to the prepended "Disable" option in the UI).</description></item>
    /// <item><description><c>1</c> to <c>SubtitleTracks.Count</c> = the <c>SelectedIndex</c> of an enabled subtitle track in the UI; the
    /// actual underlying subtitle track index is typically obtained by subtracting <c>1</c> from this value.</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty] private int _subtitleTrackIndex;

    /// <summary>
    /// The currently selected audio track index. <c>-1</c> means no track is selected.
    /// </summary>
    [ObservableProperty] private int _audioTrackIndex;

    /// <summary>
    /// The currently selected video track index. <c>-1</c> means no track is selected.
    /// </summary>
    [ObservableProperty] private int _videoTrackIndex;

    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;
    private readonly PlayerContext _playerContext;
    private bool _flyoutOpened;
    private CancellationTokenSource? _cts;

    public CompositeTrackPickerViewModel(PlayerContext playerContext, IFilesService filesService,
        ISettingsService settingsService)
    {
        _filesService = filesService;
        _settingsService = settingsService;
        _playerContext = playerContext;
        SubtitleTracks = new ObservableCollection<string>();
        AudioTracks = new ObservableCollection<string>();
        VideoTracks = new ObservableCollection<string>();

        IsActive = true;
    }

    /// <summary>
    /// Try load a subtitle in the same directory with the same name
    /// </summary>
    public async void Receive(PlaylistCurrentItemChangedMessage message)
    {
        _cts?.Cancel();
        if (MediaPlayer is not VlcMediaPlayer player) return;
        if (message.Value is not { Source: StorageFile file, MediaType: MediaPlaybackType.Video } media)
            return;

        bool subtitleInitialized = false;
        var playbackSubtitleTrackList = media.Item.Value?.SubtitleTracks;
        if (playbackSubtitleTrackList == null) return;
        if (playbackSubtitleTrackList.Count > 0) subtitleInitialized = true;
        IReadOnlyList<StorageFile> subtitles = await GetSubtitlesForFile(file, message.NeighboringFilesQuery);
        foreach (StorageFile subtitleFile in subtitles)
        {
            // Preload subtitle but don't select it
            playbackSubtitleTrackList.AddExternalSubtitle(player, subtitleFile, false);
        }

        if (!subtitleInitialized && media.Item.Value is { } playbackItem)
        {
            try
            {
                using var cts = new CancellationTokenSource();
                _cts = cts;
                await playbackItem.Media.WaitForParsed(TimeSpan.FromSeconds(5), cts.Token);
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

        TrySetSubtitleFromLanguage(playbackSubtitleTrackList, _settingsService.PersistentSubtitleLanguage);
    }

    private static void TrySetSubtitleFromLanguage(PlaybackSubtitleTrackList subtitleTrackList, string persistentLanguage)
    {
        // Check persistent subtitle value to try and select a subtitle
        if (!string.IsNullOrEmpty(persistentLanguage))
        {
            // If there is only one subtitle then select it
            if (subtitleTrackList.Count == 1)
            {
                subtitleTrackList.SelectedIndex = 0;
                return;
            }

            // Try to select the subtitle with the same language as the persistent value
            var langPreferences = persistentLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string language in langPreferences)
            {
                for (int i = 0; i < subtitleTrackList.Count; i++)
                {
                    var subtitleTrack = subtitleTrackList[i];
                    // Try to match language tag first, then language name
                    if (language == subtitleTrack.LanguageTag || language.Equals(subtitleTrack.Language, StringComparison.CurrentCultureIgnoreCase))
                    {
                        subtitleTrackList.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private async Task<IReadOnlyList<StorageFile>> GetSubtitlesForFile(StorageFile sourceFile, StorageFileQueryResult? neighboringFilesQuery = null)
    {
        IReadOnlyList<StorageFile> subtitles = Array.Empty<StorageFile>();
        string rawName = Path.GetFileNameWithoutExtension(sourceFile.Name);

        // 1. Define your separators
        char[] separators = [' ', '.', '_', '-', '[', ']', '(', ')', '{', '}', ',', ';', '"', '\''];

        // 2. Break the name into tokens, removing empty entries to avoid double wildcards (**)
        string[] tokens = rawName.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0) return subtitles;

        // If we have a neighboring files query from the playlist, use it and filter for subtitles
        if (neighboringFilesQuery != null)
        {
            try
            {
                var escapedTokens = tokens.Select(token => Regex.Escape(token)).ToList();

                // STRATEGY A: Strict "Skeleton" Match
                var strictRegexPattern = "^" + string.Join(".*", escapedTokens) + ".*$";
                IReadOnlyList<StorageFile> files = await neighboringFilesQuery.GetFilesAsync(0, 50);
                subtitles = files.Where(f =>
                       f.IsSupportedSubtitle() && Regex.IsMatch(f.Name, strictRegexPattern, RegexOptions.IgnoreCase))
                    .ToArray();
                if (subtitles.Count == 0 && tokens.Length > 1)
                {
                    // STRATEGY B: Fallback (Partial Tokens Match)
                    var fallbackPattern = "^" + string.Join(".*", escapedTokens.Take(Math.Min(escapedTokens.Count - 1, 3))) + ".*$";
                    subtitles = files.Where(f =>
                            f.IsSupportedSubtitle() && Regex.IsMatch(f.Name, fallbackPattern, RegexOptions.IgnoreCase))
                        .ToArray();
                }
            }
            catch (Exception e)
            {
                LogService.Log(e);
            }
        }
        else
        {
            // Fallback to creating a new query with subtitle filter

            // STRATEGY A: Strict "Skeleton" Match
            // "Iron.Man.2008" -> "Iron*Man*2008*"
            string strictPattern = string.Join("*", tokens) + "*";

            QueryOptions options = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedSubtitleFormats)
            {
                ApplicationSearchFilter = $"System.FileName:~\"{strictPattern}\""
            };

            var query = await _filesService.GetNeighboringFilesQueryAsync(sourceFile, options);
            if (query != null)
            {
                subtitles = await query.GetFilesAsync(0, 50);

                // STRATEGY B: Fallback (Partial Tokens Match)
                // If "Iron*Man*2008*" fails, try "Iron*Man*"
                if (subtitles.Count == 0 && tokens.Length > 1)
                {
                    string fallbackPattern = string.Join("*", tokens.Take(Math.Min(tokens.Length - 1, 3))) + "*";
                    options.ApplicationSearchFilter = $"System.FileName:~\"{fallbackPattern}\"";
                    query.ApplyNewQueryOptions(options);
                    subtitles = await query.GetFilesAsync(0, 50);
                }
            }
        }

        return subtitles;
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
            _settingsService.PersistentSubtitleLanguage = string.Empty;
        }
        else if (value < SubtitleTracks.Count)
        {
            var subtitle = ItemSubtitleTrackList[value];
            _settingsService.PersistentSubtitleLanguage =
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
    /// Adds a subtitle file to the current media. Sends a <see cref="Core.Messages.FailedToLoadSubtitleNotificationMessage"/> on failure.
    /// </summary>
    [RelayCommand]
    private async Task AddSubtitleAsync()
    {
        try
        {
            if (ItemSubtitleTrackList == null || MediaPlayer is not VlcMediaPlayer player) return;
            StorageFile? file = await _filesService.PickFileAsync(FilesHelpers.SupportedSubtitleFormats.Add("*").ToArray());
            if (file == null) return;

            ItemSubtitleTrackList.AddExternalSubtitle(player, file, true);
            Messenger.Send(new SubtitleAddedNotificationMessage(file));
        }
        catch (Exception e)
        {
            Messenger.Send(new FailedToLoadSubtitleNotificationMessage(e.Message));
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
