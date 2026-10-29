using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Contexts;
using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Core.ViewModels;

public sealed partial class CommonViewModel : ObservableRecipient,
    IRecipient<SettingChangedMessage<GeneralSettingsContext, ThemeOption>>,
    IRecipient<SettingChangedMessage<PlayerSettingsContext, bool>>,
    IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>,
    IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
{
    public Dictionary<Type, string> NavigationStates { get; }

    public bool IsAdvancedModeEnabled => _playerSettings.AdvancedMode;

    [ObservableProperty] public partial NavigationViewDisplayMode NavigationViewDisplayMode { get; set; }
    [ObservableProperty] public partial Thickness ScrollBarMargin { get; set; }
    [ObservableProperty] public partial Thickness FooterBottomPaddingMargin { get; set; }
    [ObservableProperty] public partial double FooterBottomPaddingHeight { get; set; }

    private readonly INavigationService _navigationService;
    private readonly IFilesService _filesService;
    private readonly PlayerSettingsContext _playerSettings;
    private readonly GeneralSettingsContext _generalSettings;
    private readonly IPlaylistService _playlistService;
    private readonly PlaylistsContext _playlistsContext;
    private readonly Dictionary<string, object> _pageStates;

    public CommonViewModel(INavigationService navigationService,
        IFilesService filesService,
        PlayerSettingsContext playerSettings,
        GeneralSettingsContext generalSettings,
        IPlaylistService playlistService,
        PlaylistsContext playlistsContext)
    {
        _navigationService = navigationService;
        _filesService = filesService;
        _playerSettings = playerSettings;
        _generalSettings = generalSettings;
        _playlistService = playlistService;
        _playlistsContext = playlistsContext;
        NavigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
        NavigationStates = new Dictionary<Type, string>();
        _pageStates = new Dictionary<string, object>();

        Messenger.Register<SettingChangedMessage<GeneralSettingsContext, ThemeOption>>(this);
        Messenger.Register<SettingChangedMessage<PlayerSettingsContext, bool>>(this);
        Messenger.Register<PropertyChangedMessage<NavigationViewDisplayMode>>(this);
        Messenger.Register<PropertyChangedMessage<PlayerVisibilityState>>(this);
    }

    [DynamicWindowsRuntimeCast(typeof(Frame))]
    public void Receive(SettingChangedMessage<GeneralSettingsContext, ThemeOption> message)
    {
        if (Window.Current.Content is Frame rootFrame)
        {
            rootFrame.RequestedTheme = message.NewValue.ToElementTheme();
        }
    }

    public void Receive(SettingChangedMessage<PlayerSettingsContext, bool> message)
    {
        if (message.PropertyName == nameof(PlayerSettingsContext.AdvancedMode))
        {
            OnPropertyChanged(nameof(IsAdvancedModeEnabled));
        }
    }

    public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
    {
        this.NavigationViewDisplayMode = message.NewValue;
    }

    public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
    {
        ScrollBarMargin = message.NewValue == PlayerVisibilityState.Hidden
            ? new Thickness(0)
            : (Thickness)Application.Current.Resources["ContentPageScrollBarMargin"];

        FooterBottomPaddingMargin = message.NewValue == PlayerVisibilityState.Hidden
            ? new Thickness(0)
            : (Thickness)Application.Current.Resources["ContentPageBottomMargin"];

        FooterBottomPaddingHeight = message.NewValue == PlayerVisibilityState.Hidden
            ? 0
            : (double)Application.Current.Resources["ContentPageBottomPaddingHeight"];
    }

    public void SavePageState(object state, string pageTypeName, int backStackDepth)
    {
        _pageStates[pageTypeName + backStackDepth] = state;
    }

    public bool TryGetPageState(string pageTypeName, int backStackDepth, out object? state)
    {
        return _pageStates.TryGetValue(pageTypeName + backStackDepth, out state);
    }

    [RelayCommand]
    private void PlayNext(MediaViewModel media)
    {
        Messenger.SendPlayNext(media);
    }

    [RelayCommand]
    private void AddToQueue(MediaViewModel media)
    {
        Messenger.SendAddToQueue(media);
    }

    [RelayCommand]
    private void OpenAlbum(AlbumViewModel? album)
    {
        if (album == null) return;
        _navigationService.Navigate(typeof(AlbumDetailsPageViewModel),
            new NavigationMetadata(typeof(MusicPageViewModel), album));
    }

    [RelayCommand]
    private void OpenArtist(ArtistViewModel? artist)
    {
        if (artist == null) return;
        _navigationService.Navigate(typeof(ArtistDetailsPageViewModel),
            new NavigationMetadata(typeof(MusicPageViewModel), artist));
    }

    [RelayCommand]
    private void OpenPlaylist(PlaylistViewModel? playlist)
    {
        if (playlist == null) return;
        _navigationService.Navigate(typeof(PlaylistDetailsPageViewModel),
            new NavigationMetadata(typeof(PlaylistsPageViewModel), playlist));
    }

    /// <summary>
    /// Opens a file picker for the user to select one or more media files to play.
    /// </summary>
    [RelayCommand]
    private async Task OpenFilesAsync()
    {
        try
        {
            IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync();
            if (files == null || files.Count == 0) return;
            await Messenger.PlayFilesAsync(files, _playlistService, _generalSettings.EnqueueAllFilesInFolder);
        }
        catch (Exception e)
        {
            Messenger.Send(new NotificationMessage(NotificationLevel.Error, NotificationKind.FileOpenFailed, message: e.Message));
        }
    }

}
