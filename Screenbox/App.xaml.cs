#nullable enable

using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LibVLCSharp.Shared;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Controls;
using Screenbox.Core.Messages;
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

            services.AddTransient<PlayerPageViewModel>();
            services.AddTransient<VideoViewViewModel>();
            services.AddTransient<ChapterViewModel>();
            services.AddTransient<VolumeViewModel>();
            services.AddTransient<AudioTrackSubtitleViewModel>();
            services.AddTransient<SeekBarViewModel>();
            services.AddTransient<NotificationViewModel>();
            services.AddTransient<VideosPageViewModel>();
            services.AddTransient<FolderViewPageViewModel>();
            services.AddTransient<PlayerControlsViewModel>();
            services.AddSingleton<PlaylistViewModel>(); // Shared with PlayerPage, SystemMediaTransportControls
            services.AddSingleton<SystemMediaTransportControlsViewModel>(); // Handle system controls

            services.AddSingleton<IFilesService, FilesService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IMediaPlayerService, MediaPlayerService>();
            services.AddSingleton<IMediaService, MediaService>();

            return services.BuildServiceProvider(true);
        }

        private static void ConfigureAppCenter()
        {
#if !DEBUG
            var secrets = ResourceLoader.GetForViewIndependentUse("Secrets");
            AppCenter.Start(secrets.GetString("AppCenterApiKey"),
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
            if (rootFrame.Content is not PlayerPage)
            {
                rootFrame.Navigate(typeof(PlayerPage));
            }

            Window.Current.Activate();
            WeakReferenceMessenger.Default.Send(new PlayMediaMessage(args.Files, args.NeighboringFilesQuery));
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
                rootFrame.Navigate(typeof(PlayerPage));
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
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
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
