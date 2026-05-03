#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public sealed partial class HomePageViewModel : ObservableRecipient,
    IRecipient<PlaylistCurrentItemChangedMessage>
{
    public ObservableCollection<MediaViewModel> Recent { get; }

    public SelectionViewModel Selection { get; }

    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _changeDebounceTimer;
    private readonly Dictionary<string, string> _pathToMruMappings;

    public HomePageViewModel(
        SelectionViewModel selection,
        MediaViewModelFactory mediaFactory,
        IFilesService filesService,
        ISettingsService settingsService)
    {
        Selection = selection;
        _mediaFactory = mediaFactory;
        _filesService = filesService;
        _settingsService = settingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _changeDebounceTimer = _dispatcherQueue.CreateTimer();
        _pathToMruMappings = new Dictionary<string, string>();
        Recent = new ObservableCollection<MediaViewModel>();

        Selection.IsAllSelected = Recent.GetSelectionToggleState(Selection.SelectedItemCount);
        Selection.PropertyChanged += Selection_OnPropertyChanged;

        // Activate the view model's messenger
        IsActive = true;
    }

    public void Receive(PlaylistCurrentItemChangedMessage message)
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

    private void Selection_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectionViewModel.SelectedItemCount))
        {
            Selection.IsAllSelected = Recent.GetSelectionToggleState(Selection.SelectedItemCount);
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
            }
        }
    }

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
            }
            return;
        }

        var files = await Task.WhenAll(tokens.Select(ConvertMruTokenToStorageFileAsync));
        var pairs = tokens.Zip(files, (t, f) => (Token: t, File: f)).ToList();
        var pairsToRemove = pairs.Where(p => p.File == null).ToList();

        lock (Recent)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                var (token, file) = pairs[i];
                if (file == null) continue;
                // TODO: Add support for playing playlist file from home page
                if (file.IsSupportedPlaylist()) continue;
                if (i >= Recent.Count)
                {
                    MediaViewModel media = _mediaFactory.GetSingleton(file);
                    _pathToMruMappings[media.Location] = token;
                    Recent.Add(media);
                }
                else if (Recent[i].Source is StorageFile existing)
                {
                    try
                    {
                        if (!file.IsEqual(existing)) MoveOrInsert(file, token, i);
                    }
                    catch (Exception)
                    {
                        // StorageFile.IsEqual() throws an exception
                        // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
                        // pass
                    }
                }
            }

            // Remove stale items
            while (Recent.Count > tokens.Length)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }
        }

        foreach (var (token, _) in pairsToRemove)
        {
            try
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            }
            catch (Exception e)
            {
                LogService.Log(e);
            }
        }

        // Load media details for the remaining items
        if (!loadMediaDetails) return;
        IEnumerable<Task> loadingTasks = Recent.Select(x => x.LoadDetailsAsync(_filesService));
        loadingTasks = Recent.Select(x => x.LoadThumbnailAsync()).Concat(loadingTasks);
        await Task.WhenAll(loadingTasks);
    }

    private void MoveOrInsert(StorageFile file, string token, int desiredIndex)
    {
        // Find index of the VM of the same file
        // There is no FindIndex method for ObservableCollection :(
        int existingIndex = -1;
        for (int j = desiredIndex + 1; j < Recent.Count; j++)
        {
            if (Recent[j].Source is StorageFile existingFile && file.IsEqual(existingFile))
            {
                existingIndex = j;
                break;
            }
        }

        if (existingIndex == -1)
        {
            MediaViewModel media = _mediaFactory.GetSingleton(file);
            _pathToMruMappings[media.Location] = token;
            Recent.Insert(desiredIndex, media);
        }
        else
        {
            MediaViewModel toInsert = Recent[existingIndex];
            Recent.RemoveAt(existingIndex);
            Recent.Insert(desiredIndex, toInsert);
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
            if (_pathToMruMappings.Remove(media.Location, out string token))
            {
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

    [RelayCommand]
    private void SelectItem(MediaViewModel media)
    {
        Selection.IsSelectionModeActive = true;
        Selection.SelectedItem = media;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelected(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems.OfType<MediaViewModel>().ToArray();
        if (items.Length > 0)
        {
            Messenger.SendQueueAndPlay(items[0], items, pauseIfExists: false);
        }

        selectedItems.Clear();
        ClearSelection();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems.OfType<MediaViewModel>().Reverse().ToArray();
        Messenger.SendPlayNext(items);
        selectedItems.Clear();
        ClearSelection();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddSelectedToQueue(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var items = selectedItems.OfType<MediaViewModel>().ToArray();
        Messenger.SendAddToQueue(items);
        selectedItems.Clear();
        ClearSelection();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected(IList<object>? selectedItems)
    {
        if (selectedItems is null) return;

        var copy = selectedItems.OfType<MediaViewModel>().ToArray();
        foreach (var item in copy)
        {
            Remove(item);
        }

        selectedItems.Clear();
        ClearSelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        Selection.ClearSelection();
    }

    private static async Task<StorageFile?> ConvertMruTokenToStorageFileAsync(string token)
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
        catch (Exception e)
        {
            LogService.Log(e);
            return null;
        }
    }

    private bool HasSelection(IList<object>? selectedItems) =>
        (selectedItems != null && selectedItems.Count > 0) || Selection.HasSelection;
}
