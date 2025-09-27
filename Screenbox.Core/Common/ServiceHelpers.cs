using Microsoft.Extensions.DependencyInjection;
using Screenbox.Core.Factories;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;

namespace Screenbox.Core;
public static class ServiceHelpers
{
    public static void PopulateCoreServices(ServiceCollection services)
    {
        // View models
        services.AddTransient<PlayerElementViewModel>();
        services.AddTransient<PropertyViewModel>();
        services.AddTransient<ChapterViewModel>();
        services.AddTransient<CompositeTrackPickerViewModel>();
        services.AddTransient<SeekBarViewModel>();
        services.AddTransient<VideosPageViewModel>();
        services.AddTransient<NetworkPageViewModel>();
        services.AddTransient<FolderViewPageViewModel>();
        services.AddTransient<FolderListViewPageViewModel>();
        services.AddTransient<PlayerControlsViewModel>();
        services.AddTransient<CastControlViewModel>();
        services.AddTransient<PlayerPageViewModel>();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<PlayQueuePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<PlayQueueViewModel>();
        services.AddTransient<AlbumDetailsPageViewModel>();
        services.AddTransient<ArtistDetailsPageViewModel>();
        services.AddTransient<SongsPageViewModel>();
        services.AddTransient<AlbumsPageViewModel>();
        services.AddTransient<ArtistsPageViewModel>();
        services.AddTransient<AllVideosPageViewModel>();
        services.AddTransient<MusicPageViewModel>();
        services.AddTransient<SearchResultPageViewModel>();
        services.AddTransient<NotificationViewModel>();
        services.AddTransient<LivelyWallpaperPlayerViewModel>();
        services.AddTransient<LivelyWallpaperSelectorViewModel>();
        services.AddTransient<HomePageViewModel>();
        services.AddSingleton<CommonViewModel>();   // Shared between many pages
        services.AddSingleton<VolumeViewModel>();   // Avoid thread lock
        services.AddSingleton<MediaListViewModel>(); // Global playlist

        // Misc
        services.AddTransient<LastPositionTracker>();

        // Factories
        services.AddSingleton<MediaViewModelFactory>();
        services.AddSingleton<StorageItemViewModelFactory>();
        services.AddSingleton<ArtistViewModelFactory>();
        services.AddSingleton<AlbumViewModelFactory>();
        services.AddSingleton<IPlaylistFactory, PlaylistFactory>();

        // Services
        services.AddSingleton<LibVlcService>();
        services.AddSingleton<IFilesService, FilesService>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ICastService, CastService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISystemMediaTransportControlsService, SystemMediaTransportControlsService>();
        services.AddSingleton<ILivelyWallpaperService, LivelyWallpaperService>();
        services.AddSingleton<IPlaybackControlService, PlaybackControlService>();
        services.AddSingleton<IPlaylistService, PlaylistService>();
    }
}
