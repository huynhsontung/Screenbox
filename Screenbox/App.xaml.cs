#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using LibVLCSharp.Shared;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Controls;
using Screenbox.Core;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Services;
using Screenbox.Core.ViewModels;
using Screenbox.Helpers;
using Screenbox.Pages;
using Screenbox.Services;
using Sentry;
using Sentry.Protocol;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Screenbox
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureAppCenter();
            ConfigureSentry();
            InitializeComponent();

            if (DeviceInfoHelper.IsXbox)
            {
                // Disable pointer mode on Xbox
                // https://learn.microsoft.com/en-us/windows/uwp/xbox-apps/how-to-disable-mouse-mode#xaml
                RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;

                // Use Reveal focus for 10-foot experience
                // https://learn.microsoft.com/en-us/windows/apps/design/input/gamepad-and-remote-interactions#reveal-focus
                FocusVisualKind = FocusVisualKind.Reveal;
            }

            // Disable automatic High Contrast adjustments
            // https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes#setting-highcontrastadjustment-to-none
            HighContrastAdjustment = ApplicationHighContrastAdjustment.None;

            Suspending += OnSuspending;

            IServiceProvider services = ConfigureServices();
            CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.ConfigureServices(services);
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void CoreApplication_UnhandledErrorDetected(object sender, UnhandledErrorDetectedEventArgs e)
        {
            try
            {
                e.UnhandledError.Propagate();
            }
            catch (Exception ex)
            {
                if (ex is VLCException { Message: "Could not create Direct3D11 device : No compatible adapter found." })
                {
                    WeakReferenceMessenger.Default.Send(new CriticalErrorMessage(Strings.Resources.CriticalErrorDirect3D11NotAvailable));
                    LogService.Log(ex);
                }
                else
                {
                    // Tell Sentry this was an unhandled exception
                    ex.Data[Mechanism.HandledKey] = false;
                    ex.Data[Mechanism.MechanismKey] = "CoreApplication.UnhandledErrorDetected";

                    // Capture the exception
                    SentrySdk.CaptureException(ex);

                    // Flush the event immediately
                    SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                    throw;
                }
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();
            ServiceHelpers.PopulateCoreServices(services);

            // View models
            services.AddTransient<LivelyWallpaperSelectorViewModel>(provider =>
                new LivelyWallpaperSelectorViewModel(
                    provider.GetRequiredService<ILivelyWallpaperService>(),
                    provider.GetRequiredService<IFilesService>(),
                    provider.GetRequiredService<ISettingsService>(),
                    Strings.Resources.Default, "ms-appx:///Assets/DefaultAudioVisual.png"));

            // Factories
            services.AddSingleton<Func<IVlcLoginDialog>>(_ => () => new VLCLoginDialog());

            // Services
            services.AddSingleton<IResourceService, ResourceService>();
            services.AddSingleton<INavigationService, NavigationService>(_ => new NavigationService(
                new KeyValuePair<Type, Type>(typeof(HomePageViewModel), typeof(HomePage)),
                new KeyValuePair<Type, Type>(typeof(VideosPageViewModel), typeof(VideosPage)),
                new KeyValuePair<Type, Type>(typeof(AllVideosPageViewModel), typeof(AllVideosPage)),
                new KeyValuePair<Type, Type>(typeof(MusicPageViewModel), typeof(MusicPage)),
                new KeyValuePair<Type, Type>(typeof(SongsPageViewModel), typeof(SongsPage)),
                new KeyValuePair<Type, Type>(typeof(ArtistsPageViewModel), typeof(ArtistsPage)),
                new KeyValuePair<Type, Type>(typeof(AlbumsPageViewModel), typeof(AlbumsPage)),
                new KeyValuePair<Type, Type>(typeof(NetworkPageViewModel), typeof(NetworkPage)),
                new KeyValuePair<Type, Type>(typeof(PlayQueuePageViewModel), typeof(PlayQueuePage)),
                new KeyValuePair<Type, Type>(typeof(SettingsPageViewModel), typeof(SettingsPage)),
                new KeyValuePair<Type, Type>(typeof(AlbumDetailsPageViewModel), typeof(AlbumDetailsPage)),
                new KeyValuePair<Type, Type>(typeof(ArtistDetailsPageViewModel), typeof(ArtistDetailsPage)),
                new KeyValuePair<Type, Type>(typeof(SearchResultPageViewModel), typeof(SearchResultPage)),
                new KeyValuePair<Type, Type>(typeof(ArtistSearchResultPageViewModel), typeof(ArtistSearchResultPage)),
                new KeyValuePair<Type, Type>(typeof(AlbumSearchResultPageViewModel), typeof(AlbumSearchResultPage)),
                new KeyValuePair<Type, Type>(typeof(SongSearchResultPageViewModel), typeof(SongSearchResultPage)),
                new KeyValuePair<Type, Type>(typeof(VideoSearchResultPageViewModel), typeof(VideoSearchResultPage)),
                new KeyValuePair<Type, Type>(typeof(FolderViewPageViewModel), typeof(FolderViewPage)),
                new KeyValuePair<Type, Type>(typeof(FolderListViewPageViewModel), typeof(FolderListViewPage))
            ));

            return services.BuildServiceProvider();
        }

        private static void ConfigureAppCenter()
        {
            AppCenter.Start(Secrets.AppCenterApiKey, typeof(Analytics), typeof(Crashes));
        }

        private void ConfigureSentry()
        {
            CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;

            SentrySdk.Init(options =>
            {
                options.Dsn = Secrets.SentryDsn;
                options.SampleRate = 1.0f;
                // options.StackTraceMode = StackTraceMode.Enhanced;    // Not supported in UWP
                options.IsGlobalModeEnabled = true;
                options.AutoSessionTracking = true;
                options.Release = $"screenbox@{Package.Current.Id.Version.ToFormattedString(3)}";
            });

            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("device_family", DeviceInfoHelper.DeviceFamily);
            });
        }

        private void SetMinWindowSize()
        {
            //var view = ApplicationView.GetForCurrentView();
            //view.SetPreferredMinSize(new Size(480, 270));
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            SentrySdk.AddBreadcrumb("File activated", category: "activation", type: "user", data: new Dictionary<string, string>
            {
                { "PreviousExecutionState", args.PreviousExecutionState.ToString() }
            });

            Frame rootFrame = InitRootFrame();
            if (rootFrame.Content is not MainPage)
            {
                rootFrame.Navigate(typeof(MainPage), true);
            }

            Window.Current.Activate();
            
            // Auto enter full screen if setting is enabled and we're not already in a special mode
            var settings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ISettingsService>();
            if (settings.PlayerAutoFullScreen)
            {
                var view = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                if (!view.IsFullScreenMode && view.ViewMode == Windows.UI.ViewManagement.ApplicationViewMode.Default)
                {
                    view.TryEnterFullScreenMode();
                }
            }
            
            WeakReferenceMessenger.Default.Send(new PlayFilesMessage(args.Files, args.NeighboringFilesQuery));
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            SentrySdk.AddBreadcrumb("Launched", category: "lifecycle", data: new Dictionary<string, string>
            {
                { "PrelaunchActivated", e.PrelaunchActivated.ToString() },
                { "PreviousExecutionState", e.PreviousExecutionState.ToString() }
            });

            Frame rootFrame = InitRootFrame();
            LibVLCSharp.Shared.Core.Initialize();

            if (e.PrelaunchActivated) return;
            CoreApplication.EnablePrelaunch(true);
            if (rootFrame.Content == null)
            {
                SetMinWindowSize();
                rootFrame.Navigate(typeof(MainPage));
            }

            // Ensure the current window is active
            Window.Current.Activate();

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //DebugSettings.EnableFrameRateCounter = true;
                //DebugSettings.EnableRedrawRegions = true;
                //DebugSettings.FailFastOnErrors = true;
                //DebugSettings.IsBindingTracingEnabled = true;
                //DebugSettings.IsOverdrawHeatMapEnabled = true;
                //DebugSettings.IsTextPerformanceVisualizationEnabled = true;
            }
#endif
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
            SentrySdk.AddBreadcrumb("Suspending", category: "lifecycle");
            IReadOnlyCollection<Task> tasks = WeakReferenceMessenger.Default.Send<SuspendingMessage>().Responses;
            await Task.WhenAll(tasks);
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
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

                // Turn off overscan on Xbox
                // https://learn.microsoft.com/en-us/windows/uwp/xbox-apps/turn-off-overscan
                if (DeviceInfoHelper.IsXbox)
                {
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
                        .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
                }

                // Check for RTL flow direction
                if (GlobalizationHelper.IsRightToLeftLanguage)
                {
                    rootFrame.FlowDirection = FlowDirection.RightToLeft;
                }

                var settings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ISettingsService>();
                rootFrame.RequestedTheme = settings.Theme.ToElementTheme();

                // Register a handler for when the theme mode changes
                rootFrame.ActualThemeChanged += OnActualThemeChanged;

                TitleBarHelper.SetCaptionButtonColors(rootFrame);
            }

            return rootFrame;
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            TitleBarHelper.SetCaptionButtonColors(sender);
        }
    }
}
