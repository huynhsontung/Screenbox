#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Factories;
using Screenbox.Pages;
using Screenbox.Services;
using Screenbox.ViewModels;

namespace Screenbox
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static App DerivedCurrent => (App)Current;

        public static IServiceProvider Services => DerivedCurrent._services;

        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureAppCenter();
            _services = ConfigureServices();
            InitializeComponent();
            Suspending += OnSuspending;
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // View models
            services.AddTransient<PlayerElementViewModel>();
            services.AddTransient<PlayerInteractionViewModel>();
            services.AddTransient<PropertyViewModel>();
            services.AddTransient<ChapterViewModel>();
            services.AddTransient<AudioTrackSubtitleViewModel>();
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
            services.AddTransient<PlaylistViewModel>();
            services.AddTransient<AlbumDetailsPageViewModel>();
            services.AddTransient<ArtistDetailsPageViewModel>();
            services.AddTransient<SongsPageViewModel>();
            services.AddSingleton<NotificationViewModel>(); // Shared between multiple notification views
            services.AddSingleton<CommonViewModel>();   // Shared between many pages
            services.AddSingleton<VolumeViewModel>();   // Avoid thread lock
            services.AddSingleton<HomePageViewModel>(); // Prevent recent media reload on every page navigation
            services.AddSingleton<MusicPageViewModel>(); // Prevent song library reload on every page navigation
            services.AddSingleton<MediaListViewModel>(); // Global playlist

            // Factories
            services.AddSingleton<MediaViewModelFactory>();
            services.AddSingleton<StorageItemViewModelFactory>();
            services.AddSingleton<ArtistViewModelFactory>();
            services.AddSingleton<AlbumViewModelFactory>();

            // Services
            services.AddSingleton<LibVlcService>();
            services.AddSingleton<IFilesService, FilesService>();
            services.AddSingleton<ILibraryService, LibraryService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IMediaService, MediaService>();
            services.AddSingleton<ICastService, CastService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ISystemMediaTransportControlsService, SystemMediaTransportControlsService>();
            services.AddSingleton<INavigationService, NavigationService>(_ => new NavigationService(
                new KeyValuePair<Type, Type>(typeof(HomePageViewModel), typeof(HomePage)),
                new KeyValuePair<Type, Type>(typeof(VideosPageViewModel), typeof(VideosPage)),
                new KeyValuePair<Type, Type>(typeof(MusicPageViewModel), typeof(MusicPage)),
                new KeyValuePair<Type, Type>(typeof(SongsPageViewModel), typeof(SongsPage)),
                new KeyValuePair<Type, Type>(typeof(ArtistsPageViewModel), typeof(ArtistsPage)),
                new KeyValuePair<Type, Type>(typeof(AlbumsPageViewModel), typeof(AlbumsPage)),
                new KeyValuePair<Type, Type>(typeof(NetworkPageViewModel), typeof(NetworkPage)),
                new KeyValuePair<Type, Type>(typeof(PlayQueuePageViewModel), typeof(PlayQueuePage)),
                new KeyValuePair<Type, Type>(typeof(SettingsPageViewModel), typeof(SettingsPage)),
                new KeyValuePair<Type, Type>(typeof(AlbumDetailsPageViewModel), typeof(AlbumDetailsPage)),
                new KeyValuePair<Type, Type>(typeof(ArtistDetailsPageViewModel), typeof(ArtistDetailsPage)),
                new KeyValuePair<Type, Type>(typeof(FolderViewPageViewModel), typeof(FolderViewPage)),
                new KeyValuePair<Type, Type>(typeof(FolderListViewPageViewModel), typeof(FolderListViewPage))
            ));

            return services.BuildServiceProvider();
        }

        private static void ConfigureAppCenter()
        {
#if !DEBUG
            AppCenter.Start(Secrets.AppCenterApiKey,
                typeof(Analytics), typeof(Crashes));
#endif
        }

        private void SetMinWindowSize()
        {
            //var view = ApplicationView.GetForCurrentView();
            //view.SetPreferredMinSize(new Size(480, 270));
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            var rootFrame = InitRootFrame();
            if (rootFrame.Content is not MainPage)
            {
                rootFrame.Navigate(typeof(MainPage), true);
            }

            Window.Current.Activate();
            WeakReferenceMessenger.Default.Send(new PlayFilesWithNeighborsMessage(args.Files, args.NeighboringFilesQuery));
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = InitRootFrame();
            LibVLCSharp.Shared.Core.Initialize();

            if (e.PrelaunchActivated == false)
            {
                Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
            }

            if (rootFrame.Content == null)
            {
                SetMinWindowSize();
                rootFrame.Navigate(typeof(MainPage));
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            IReadOnlyCollection<Task> tasks = WeakReferenceMessenger.Default.Send<SuspendingMessage>().Responses;
            await Task.WhenAll(tasks);
            deferral.Complete();
        }

        private Frame InitRootFrame()
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (Window.Current.Content is not Frame rootFrame)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                SetMinWindowSize();
            }

            return rootFrame;
        }
    }
}
