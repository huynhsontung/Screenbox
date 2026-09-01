using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Contexts;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class HomePageViewModel : ObservableRecipient,
    IRecipient<QueueCurrentItemChangedMessage>
{
    public ObservableCollection<MediaViewModel> Recent => _recentContext.Recent;

    public SelectionViewModel Selection { get; }

    [ObservableProperty]
    public partial MediaViewModel? ContextMedia { get; set; }

    private readonly RecentContext _recentContext;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _changeDebounceTimer;
    private readonly ILogger<HomePageViewModel> _logger;

    public HomePageViewModel(
        RecentContext recentContext,
        SelectionViewModel selection,
        MediaViewModelFactory mediaFactory,
        IFilesService filesService,
        ISettingsService settingsService,
        ILogger<HomePageViewModel> logger)
    {
        Selection = selection;
        _recentContext = recentContext;
        _mediaFactory = mediaFactory;
        _filesService = filesService;
        _settingsService = settingsService;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _changeDebounceTimer = _dispatcherQueue.CreateTimer();

        Selection.SetItemsSource(Recent);
        Selection.PropertyChanged += Selection_OnPropertyChanged;

        Messenger.Register<QueueCurrentItemChangedMessage>(this);
    }

    public void Receive(QueueCurrentItemChangedMessage message)
    {
        if (_settingsService.ShowRecent)
        {
            _changeDebounceTimer.Debounce(DebouncedAction, TimeSpan.FromMilliseconds(100));

            async void DebouncedAction()
            {
                await UpdateRecentMediaListAsync(false).ConfigureAwait(false);
            }
        }
    }

    public async void OnLoaded()
    {
        await UpdateContentAsync();
    }

    private void Selection_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectionViewModel.IsAllSelected))
        {
            PlaySelectedCommand.NotifyCanExecuteChanged();
            PlaySelectedNextCommand.NotifyCanExecuteChanged();
            AddSelectedToQueueCommand.NotifyCanExecuteChanged();
            RemoveSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void OpenUrl(Uri? url)
    {
        if (url == null) return;
        Messenger.Send(new PlayMediaMessage(url));
    }

    private async Task UpdateContentAsync()
    {
        // Update recent media
        if (_settingsService.ShowRecent)
        {
            await UpdateRecentMediaListAsync(true);
        }
        else
        {
            lock (Recent)
            {
                Recent.Clear();
                _recentContext.PathToMruMappings.Clear();
                _recentContext.TokenToMediaMappings.Clear();
                _recentContext.IsLoaded = true;
            }
        }
    }

    [DynamicWindowsRuntimeCast(typeof(StorageFile))]
    private async Task UpdateRecentMediaListAsync(bool loadMediaDetails)
    {
        // Assume UI Thread
        string[] tokens = StorageApplicationPermissions.MostRecentlyUsedList.Entries
            .OrderByDescending(x => x.Metadata)
            .Select(x => x.Token)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();

        if (tokens.Length == 0)
        {
            lock (Recent)
            {
                Recent.Clear();
                _recentContext.PathToMruMappings.Clear();
                _recentContext.TokenToMediaMappings.Clear();
                _recentContext.IsLoaded = true;
            }
            return;
        }

        // Fast path: for tokens already known and cached in _recentContext, reuse existing MediaViewModel and StorageFile
        var fetchTasks = tokens.Select(async token =>
        {
            if (_recentContext.TokenToMediaMappings.TryGetValue(token, out var cachedMedia) &&
                cachedMedia.Source is StorageFile cachedFile)
            {
                return (Token: token, File: (StorageFile?)cachedFile, Media: (MediaViewModel?)cachedMedia);
            }

            StorageFile? file = await ConvertMruTokenToStorageFileAsync(token).ConfigureAwait(false);
            return (Token: token, File: file, Media: (MediaViewModel?)null);
        });

        var results = await Task.WhenAll(fetchTasks).ConfigureAwait(true);

        var validPairs = new List<(string Token, StorageFile File, MediaViewModel Media)>();
        var tokensToRemove = new List<string>();

        foreach (var (token, file, cachedMedia) in results)
        {
            if (file == null)
            {
                tokensToRemove.Add(token);
                continue;
            }

            // TODO: Add support for playing playlist file from home page
            if (file.IsSupportedPlaylist())
            {
                continue;
            }

            MediaViewModel media = cachedMedia ?? _mediaFactory.GetOrCreate(file);
            validPairs.Add((token, file, media));
        }

        var targetList = validPairs.Select(p => p.Media).ToList();

        lock (Recent)
        {
            // Update mappings for all valid pairs
            _recentContext.PathToMruMappings.Clear();
            _recentContext.TokenToMediaMappings.Clear();
            foreach (var (token, _, media) in validPairs)
            {
                _recentContext.PathToMruMappings[media.Location] = token;
                _recentContext.TokenToMediaMappings[token] = media;
            }

            // Sync the observable collection in-place
            Recent.SyncItems(targetList);
            _recentContext.IsLoaded = true;
        }

        // Remove stale/inaccessible MRU tokens
        foreach (string token in tokensToRemove)
        {
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to remove stale MRU token '{Token}'.", token);
            }
        }

        // Load media details & thumbnails for any items that need them
        if (!loadMediaDetails) return;
        var loadingTasks = new List<Task>();
        foreach (MediaViewModel media in Recent)
        {
            if (!media.DetailsLoaded)
            {
                loadingTasks.Add(SafeLoadDetailsAsync(media));
            }

            if (media.Thumbnail == null)
            {
                loadingTasks.Add(SafeLoadThumbnailAsync(media));
            }
        }

        if (loadingTasks.Count > 0)
        {
            await Task.WhenAll(loadingTasks);
        }
    }

    private async Task SafeLoadDetailsAsync(MediaViewModel media)
    {
        try
        {
            await media.LoadDetailsAsync(_filesService);
        }
        catch (ArgumentException)
        {
            // Expected: the underlying StorageFile (e.g. from MRU) may be stale and
            // throw ArgumentException. Ignore silently — this is a known bad state.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load recent media details for '{Path}'.", media.Location);
        }
    }

    private async Task SafeLoadThumbnailAsync(MediaViewModel media)
    {
        try
        {
            await media.LoadThumbnailAsync();
        }
        catch (ArgumentException)
        {
            // Expected: the underlying StorageFile (e.g. from MRU) may be stale and
            // throw ArgumentException. Ignore silently — this is a known bad state.
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load a thumbnail for '{Path}'.", media.Location);
        }
    }

    [RelayCommand]
    private void Play(MediaViewModel media)
    {
        if (media.IsMediaActive)
        {
            Messenger.Send(new TogglePlayPauseMessage(false));
        }
        else
        {
            Messenger.Send(new PlayMediaMessage(media, false));
        }
    }

    [RelayCommand]
    private void Remove(MediaViewModel media)
    {
        lock (Recent)
        {
            Recent.Remove(media);
            if (_recentContext.PathToMruMappings.Remove(media.Location, out var token))
            {
                _recentContext.TokenToMediaMappings.Remove(token);
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            }
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        StorageFolder? folder = await _filesService.PickFolderAsync();
        if (folder == null) return;
        IReadOnlyList<IStorageItem> items = await _filesService.GetSupportedItems(folder).GetItemsAsync();
        IStorageFile[] files = items.OfType<IStorageFile>().ToArray();
        if (files.Length == 0) return;
        Messenger.Send(new PlayMediaMessage(files));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelected()
    {
        var items = Selection.GetSelectedItems<MediaViewModel>().ToArray();
        if (items.Length > 0)
        {
            Messenger.SendQueueAndPlay(items[0], items, pauseIfExists: false);
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext()
    {
        var items = Selection.GetSelectedItems<MediaViewModel>();
        items.Reverse();
        Messenger.SendPlayNext(items.ToArray());
        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddSelectedToQueue()
    {
        var items = Selection.GetSelectedItems<MediaViewModel>().ToArray();
        Messenger.SendAddToQueue(items);
        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected()
    {
        var copy = Selection.GetSelectedItems<MediaViewModel>().ToArray();
        foreach (var item in copy)
        {
            Remove(item);
        }

        Selection.DisableSelectionMode();
    }

    private async Task<StorageFile?> ConvertMruTokenToStorageFileAsync(string token)
    {
        try
        {
            return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token,
                AccessCacheOptions.SuppressAccessTimeUpdate);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (System.IO.FileNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to resolve MRU token '{Token}' to a file.", token);
            return null;
        }
    }

    private bool HasSelection() => Selection.SelectedRanges.Count > 0;
}

