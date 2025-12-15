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
using AudioTrack = Screenbox.Core.Playback.AudioTrack;
using SubtitleTrack = Screenbox.Core.Playback.SubtitleTrack;
using VideoTrack = Screenbox.Core.Playback.VideoTrack;

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

    [ObservableProperty] private int _subtitleTrackIndex;
    [ObservableProperty] private int _audioTrackIndex;
    [ObservableProperty] private int _videoTrackIndex;
    private readonly IFilesService _filesService;
    private readonly IResourceService _resourceService;
    private readonly ISettingsService _settingsService;
    private readonly PlayerContext _playerContext;
    private bool _flyoutOpened;
    private CancellationTokenSource? _cts;

    public CompositeTrackPickerViewModel(PlayerContext playerContext, IFilesService filesService,
        IResourceService resourceService, ISettingsService settingsService)
    {
        _filesService = filesService;
        _resourceService = resourceService;
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
        if (ItemSubtitleTrackList != null && value >= 0 && value < SubtitleTracks.Count)
        {
            ItemSubtitleTrackList.SelectedIndex = value - 1;

            if (_flyoutOpened)
            {
                if (value == 0)
                {
                    _settingsService.PersistentSubtitleLanguage = string.Empty;
                }
                else
                {
                    var subtitle = ItemSubtitleTrackList[ItemSubtitleTrackList.SelectedIndex];
                    _settingsService.PersistentSubtitleLanguage =
                        $"{subtitle.LanguageTag},{subtitle.Language},{LanguageHelper.GetPreferredLanguage().Substring(0, 2)}";
                }
            }
        }

    }

    partial void OnAudioTrackIndexChanged(int value)
    {
        if (ItemAudioTrackList != null && value >= 0 && value < AudioTracks.Count)
            ItemAudioTrackList.SelectedIndex = value;
    }

    partial void OnVideoTrackIndexChanged(int value)
    {
        if (ItemVideoTrackList != null && value >= 0 && value < VideoTracks.Count)
            ItemVideoTrackList.SelectedIndex = value;
    }

    [RelayCommand]
    private async Task AddSubtitle()
    {
        if (ItemSubtitleTrackList == null || MediaPlayer is not VlcMediaPlayer player) return;
        try
        {
            StorageFile? file = await _filesService.PickFileAsync(FilesHelpers.SupportedSubtitleFormats.Add("*").ToArray());
            if (file == null) return;

            ItemSubtitleTrackList.AddExternalSubtitle(player, file, true);
            Messenger.Send(new SubtitleAddedNotificationMessage(file));
        }
        catch (Exception e)
        {
            Messenger.Send(new ErrorMessage(
                _resourceService.GetString(ResourceName.FailedToLoadSubtitleNotificationTitle), e.ToString()));
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
        AudioTracks.Clear();
        ItemAudioTrackList.Refresh();
        if (ItemAudioTrackList.Count <= 0) return;

        for (int index = 0; index < ItemAudioTrackList.Count; index++)
        {
            AudioTrack audioTrack = ItemAudioTrackList[index];
            string defaultTrackLabel = _resourceService.GetString(ResourceName.TrackIndex, index + 1);
            AudioTracks.Add(string.IsNullOrEmpty(audioTrack.Label) ? defaultTrackLabel : audioTrack.Label);
        }
    }

    private void UpdateVideoTrackList()
    {
        if (ItemVideoTrackList == null) return;
        VideoTracks.Clear();
        ItemVideoTrackList.Refresh();
        if (ItemVideoTrackList.Count <= 0) return;

        for (int index = 0; index < ItemVideoTrackList.Count; index++)
        {
            VideoTrack videoTrack = ItemVideoTrackList[index];
            string defaultTrackLabel = _resourceService.GetString(ResourceName.TrackIndex, index + 1);
            VideoTracks.Add(string.IsNullOrEmpty(videoTrack.Label) ? defaultTrackLabel : videoTrack.Label);
        }
    }

    private void UpdateSubtitleTrackList()
    {
        if (ItemSubtitleTrackList == null) return;
        SubtitleTracks.Clear();
        SubtitleTracks.Add(_resourceService.GetString(ResourceName.Disable));
        if (ItemSubtitleTrackList.Count <= 0) return;

        for (int index = 0; index < ItemSubtitleTrackList.Count; index++)
        {
            SubtitleTrack subtitleTrack = ItemSubtitleTrackList[index];
            string defaultTrackLabel = _resourceService.GetString(ResourceName.TrackIndex, index + 1);
            SubtitleTracks.Add(string.IsNullOrEmpty(subtitleTrack.Label) ? defaultTrackLabel : subtitleTrack.Label);
        }
    }
}
