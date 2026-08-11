#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.Storage.Search;
using Windows.System;

namespace Screenbox.Core.ViewModels;

public partial class FolderViewPageViewModel : ObservableRecipient,
    IRecipient<RefreshFolderMessage>
{
    public string TitleText => BreadcrumbLocations.LastOrDefault()?.Name ?? string.Empty;

    public ObservableCollection<StorageItemViewModel> Items { get; } = [];

    // Due to some regression in BreadcrumbBar, each breadcrumb item must be a string,
    // so we need to maintain a separate list of StorageFolder for navigation.
    public string[] Breadcrumbs { get; private set; } = [];

    public StorageFolder[] BreadcrumbLocations { get; private set; } = [];

    internal NavigationMetadata? NavData { get; private set; }

    [ObservableProperty] public partial StorageItemViewModel? ContextItem { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }

    private readonly IFilesService _filesService;
    private readonly INavigationService _navigationService;
    private readonly StorageItemViewModelFactory _storageVmFactory;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _loadingTimer;
    private readonly List<MediaViewModel> _playableItems = [];
    private bool _isActive;
    private object? _source;

    public FolderViewPageViewModel(IFilesService filesService, INavigationService navigationService,
        StorageItemViewModelFactory storageVmFactory)
    {
        _filesService = filesService;
        _storageVmFactory = storageVmFactory;
        _navigationService = navigationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _loadingTimer = _dispatcherQueue.CreateTimer();

        Messenger.Register<RefreshFolderMessage>(this);
    }

    public void Receive(RefreshFolderMessage message)
    {
        if (!_isActive) return;
        _dispatcherQueue.TryEnqueue(RefreshFolderContent);
    }

    public async Task OnNavigatedTo(object? parameter)
    {
        _isActive = true;
        _source = parameter;
        NavData = parameter as NavigationMetadata;
        await FetchContentAsync(NavData?.Parameter ?? parameter);
    }

    public void OnBreadcrumbBarItemClicked(int index)
    {
        IReadOnlyList<StorageFolder> crumbs = BreadcrumbLocations.Take(index + 1).ToArray();
        if (NavData != null)
        {
            if (index == 0)
            {
                _navigationService.Navigate(NavData.RootViewModelType);
            }
            else
            {
                _navigationService.Navigate(typeof(FolderViewPageViewModel),
                    new NavigationMetadata(NavData.RootViewModelType, crumbs));
            }
        }
        else
        {
            _navigationService.Navigate(typeof(FolderViewPageViewModel),
                new NavigationMetadata(typeof(FolderViewPageViewModel), crumbs));
        }
    }

    private async Task FetchContentAsync(object? parameter)
    {
        switch (parameter)
        {
            case IReadOnlyList<StorageFolder> { Count: > 0 } breadcrumbs:
                BreadcrumbLocations = [.. breadcrumbs];
                Breadcrumbs = breadcrumbs.Select(f => f.DisplayName).ToArray();
                await FetchFolderContentAsync(breadcrumbs.Last());
                break;
            case StorageLibrary library:
                await FetchFolderContentAsync(library);
                break;
            case StorageFileQueryResult queryResult:
                await FetchQueryItemAsync(queryResult);
                break;
            case "VideosLibrary":   // Special case for VideosPage
                // VideosPage needs to serialize navigation state so it cannot set nav data
                try
                {
                    BreadcrumbLocations = [SystemInformation.IsXbox ? KnownFolders.RemovableDevices : KnownFolders.VideosLibrary];
                    Breadcrumbs = [BreadcrumbLocations[0].DisplayName];
                    NavData = new NavigationMetadata(typeof(VideosPageViewModel), BreadcrumbLocations);
                    await FetchFolderContentAsync(BreadcrumbLocations[0]);
                }
                catch (Exception)
                {
                    // pass
                }
                break;
        }
    }

    public void OnNavigatedFrom()
    {
        _isActive = false;
    }

    protected virtual void Navigate(object? parameter = null)
    {
        // _navigationService.NavigateExisting(typeof(FolderViewPageViewModel), parameter);
        _navigationService.Navigate(typeof(FolderViewPageViewModel),
            new NavigationMetadata(NavData?.RootViewModelType ?? typeof(FolderViewPageViewModel), parameter));
    }

    [RelayCommand]
    private void Play(StorageItemViewModel? item)
    {
        if (item?.Media == null) return;
        Messenger.SendQueueAndPlay(item.Media, _playableItems, true);
    }

    [RelayCommand]
    private void PlayNext(StorageItemViewModel? item)
    {
        if (item?.Media == null) return;
        Messenger.SendPlayNext(item.Media);
    }

    [RelayCommand]
    private void AddToQueue(StorageItemViewModel? item)
    {
        if (item?.Media == null) return;
        Messenger.SendAddToQueue(item.Media);
    }

    [RelayCommand]
    private void Click(StorageItemViewModel? item)
    {
        if (item?.Media != null)
        {
            Play(item);
        }
        else if (item?.StorageItem is StorageFolder folder)
        {
            StorageFolder[] crumbs = BreadcrumbLocations.Append(folder).ToArray();
            Navigate(crumbs);
        }
    }

    private async Task FetchQueryItemAsync(StorageFileQueryResult query)
    {
        Items.Clear();
        _playableItems.Clear();

        uint fetchIndex = 0;
        while (_isActive)
        {
            _loadingTimer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(800));
            IReadOnlyList<StorageFile> items = await query.GetFilesAsync(fetchIndex, 30);
            if (items.Count == 0) break;
            fetchIndex += (uint)items.Count;
            foreach (StorageFile storageFile in items)
            {
                StorageItemViewModel item = _storageVmFactory.GetInstance(storageFile);
                Items.Add(item);
                if (item.Media != null) _playableItems.Add(item.Media);
            }
        }

        _loadingTimer.Stop();
        IsLoading = false;
        IsEmpty = Items.Count == 0;
    }

    private async Task FetchFolderContentAsync(StorageFolder folder)
    {
        Items.Clear();
        _playableItems.Clear();

        StorageItemQueryResult itemQuery = _filesService.GetSupportedItems(folder);
        uint fetchIndex = 0;
        while (_isActive)
        {
            _loadingTimer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(800));
            IReadOnlyList<IStorageItem> items = await itemQuery.GetItemsAsync(fetchIndex, 30);
            if (items.Count == 0) break;
            fetchIndex += (uint)items.Count;
            foreach (IStorageItem storageItem in items)
            {
                StorageItemViewModel item = _storageVmFactory.GetInstance(storageItem);
                Items.Add(item);
                if (item.Media != null) _playableItems.Add(item.Media);
            }
        }

        _loadingTimer.Stop();
        IsLoading = false;
        IsEmpty = Items.Count == 0;
    }

    private async Task FetchFolderContentAsync(StorageLibrary library)
    {
        if (library.Folders.Count <= 0)
        {
            IsEmpty = true;
            return;
        }

        if (library.Folders.Count == 1)
        {
            // StorageLibrary is always the root
            // Fetch content of the only folder if applicable
            StorageFolder folder = library.Folders[0];
            BreadcrumbLocations = [folder];
            Breadcrumbs = [folder.DisplayName];
            await FetchFolderContentAsync(folder);
        }
        else
        {
            Items.Clear();
            foreach (StorageFolder folder in library.Folders)
            {
                StorageItemViewModel item = _storageVmFactory.GetInstance(folder);
                Items.Add(item);
                await item.UpdateCaptionAsync();
            }

            IsEmpty = Items.Count == 0;
        }
    }

    private async void RefreshFolderContent()
    {
        await FetchContentAsync(NavData?.Parameter ?? _source);
    }
}
