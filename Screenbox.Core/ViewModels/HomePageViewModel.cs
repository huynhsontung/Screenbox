using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Contexts;
using Screenbox.Core.Coordinators;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using Windows.Storage;

namespace Screenbox.Core.ViewModels;

public sealed partial class HomePageViewModel : ObservableRecipient
{
    public ObservableCollection<RecentItemViewModel> Recent => _recentContext.Recent;

    public SelectionViewModel Selection { get; }

    [ObservableProperty]
    public partial RecentItemViewModel? ContextMedia { get; set; }

    private readonly RecentContext _recentContext;
    private readonly IRecentMediaCoordinator _recentMediaCoordinator;
    private readonly MediaViewModelFactory _mediaFactory;
    private readonly IFilesService _filesService;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IQueueService _queueService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _changeDebounceTimer;
    private readonly ILogger<HomePageViewModel> _logger;

    public HomePageViewModel(
        RecentContext recentContext,
        IRecentMediaCoordinator recentMediaCoordinator,
        SelectionViewModel selection,
        MediaViewModelFactory mediaFactory,
        IFilesService filesService,
        GeneralSettingsContext generalSettings,
        IQueueService queueService,
        ILogger<HomePageViewModel> logger)
    {
        Selection = selection;
        _recentContext = recentContext;
        _recentMediaCoordinator = recentMediaCoordinator;
        _mediaFactory = mediaFactory;
        _filesService = filesService;
        _generalSettings = generalSettings;
        _queueService = queueService;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _changeDebounceTimer = _dispatcherQueue.CreateTimer();

        Selection.SetItemsSource(Recent);
        Selection.PropertyChanged += Selection_OnPropertyChanged;
    }

    public async void OnLoaded()
    {
        if (_generalSettings.ShowRecent)
        {
            if (!_recentContext.IsLoaded)
            {
                await _recentMediaCoordinator.LoadAsync();
            }

            await _recentMediaCoordinator.LoadDetailsAndThumbnailsAsync();
        }
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
        Messenger.Send(new PlayMediaMessage(_mediaFactory.Create(url)));
    }

    [RelayCommand]
    private void Play(RecentItemViewModel? item)
    {
        item?.Play();
    }

    [RelayCommand]
    private void PlayNext(RecentItemViewModel? item)
    {
        item?.PlayNext();
    }

    [RelayCommand]
    private void AddToQueue(RecentItemViewModel? item)
    {
        item?.AddToQueue();
    }

    [RelayCommand]
    private async Task Remove(RecentItemViewModel? item)
    {
        if (item is null) return;
        await _recentMediaCoordinator.RemoveAsync(item);
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        StorageFolder? folder = await _filesService.PickFolderAsync();
        if (folder == null) return;
        await Messenger.PlayFilesAsync(new IStorageItem[] { folder }, _queueService, _generalSettings.EnqueueAllFilesInFolder);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelected()
    {
        var selected = Selection.GetSelectedItems<RecentItemViewModel>();
        if (selected.Count == 1)
        {
            selected[0].Play();
        }
        else if (selected.Count > 1)
        {
            var items = selected.SelectMany(r => r.GetPlayableItems()).ToArray();
            if (items.Length > 0)
            {
                Messenger.SendQueueAndPlay(
                    items[0],
                    items,
                    pauseIfExists: false,
                    origin: PlaylistOrigin.ForAdHocPlaylist(items[0].Name));
            }
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void PlaySelectedNext()
    {
        var items = Selection
            .GetSelectedItems<RecentItemViewModel>()
            .SelectMany(r => r.GetPlayableItems())
            .Reverse()
            .ToArray();
        if (items.Length > 0)
        {
            Messenger.SendPlayNext(items);
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddSelectedToQueue()
    {
        var items = Selection
            .GetSelectedItems<RecentItemViewModel>()
            .SelectMany(r => r.GetPlayableItems())
            .ToArray();
        if (items.Length > 0)
        {
            Messenger.SendAddToQueue(items);
        }

        Selection.DisableSelectionMode();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task RemoveSelected()
    {
        var copy = Selection.GetSelectedItems<RecentItemViewModel>().ToArray();
        foreach (var item in copy)
        {
            await _recentMediaCoordinator.RemoveAsync(item);
        }

        Selection.DisableSelectionMode();
    }

    private bool HasSelection() => Selection.SelectedRanges.Count > 0;
}
