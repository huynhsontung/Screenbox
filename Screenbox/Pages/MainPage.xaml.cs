#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using Sentry;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page, IContentFrame
    {
        public Type ContentSourcePageType => ContentFrame.SourcePageType;

        public object? FrameContent => ContentFrame.Content;

        public bool CanGoBack => ContentFrame.CanGoBack;

        private MainPageViewModel ViewModel => (MainPageViewModel)DataContext;

        private readonly Dictionary<string, Type> _pages;

        public MainPage()
        {
            InitializeComponent();

            // Hide default title bar.
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);

            // Register a handler for when the size of the overlaid caption control changes.
            // For example, when the app moves to a screen with a different DPI.
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            _pages = new Dictionary<string, Type>
            {
                { "home", typeof(HomePage) },
                { "videos", typeof(VideosPage) },
                { "music", typeof(MusicPage) },
                { "queue", typeof(PlayQueuePage) },
                { "network", typeof(NetworkPage) },
                { "settings", typeof(SettingsPage) }
            };

            DataContext = Ioc.Default.GetRequiredService<MainPageViewModel>();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ContentFrame.Navigating += ContentFrame_Navigating;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            // In RTL languages, Grid is flipped automatically.
            // Left is always the side without the system controls.
            // Left padding should only be set if we pin flow direction on the title bar.
            // LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(Math.Max(sender.SystemOverlayLeftInset, sender.SystemOverlayRightInset));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PlayerFrame.Navigate(typeof(PlayerPage), e.Parameter);
            if (e.Parameter is true)
            {
                ViewModel.PlayerVisible = true;
            }

            // NavView remembers if the pane was open last time
            ViewModel.IsPaneOpen = NavView.IsPaneOpen;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            e.Handled = ViewModel.ProcessGamepadKeyDown(e.Key);
            base.OnKeyDown(e);
        }

        public void GoBack()
        {
            TryGoBack();
        }

        public void NavigateContent(Type pageType, object? parameter)
        {
            ViewModel.PlayerVisible = false;
            ContentFrame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.Dispatcher.AcceleratorKeyActivated += CoreDispatcher_AcceleratorKeyActivated;
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            ViewModel.NavigationViewDisplayMode = (NavigationViewDisplayMode)NavView.DisplayMode;
            if (!ViewModel.PlayerVisible)
            {
                SetTitleBar();
                NavView.SelectedItem = NavView.MenuItems[0];
                _ = ViewModel.FetchLibraries();
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlayerVisible))
            {
                if (!ViewModel.PlayerVisible)
                {
                    SetTitleBar();
                    if (ContentFrame.Content == null)
                    {
                        NavView.SelectedItem = NavView.MenuItems[0];
                        _ = ViewModel.FetchLibraries();
                    }
                }

                UpdateNavigationViewState(NavView.DisplayMode, NavView.IsPaneOpen);
            }
        }

        private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            SentrySdk.AddBreadcrumb(string.Empty, category: "navigation", type: "navigation", data: new Dictionary<string, string> {
                { "from", ((Frame)sender).CurrentSourcePageType?.Name ?? string.Empty },
                { "to", e.SourcePageType?.Name ?? string.Empty },
                { "NavigationMode", e.NavigationMode.ToString()  }
            });
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName, e.Exception);
        }

        private void NavView_SelectionChanged(muxc.NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavView_Navigate("settings");
            }
            else if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag);
            }
        }

        private void NavView_Navigate(string navItemTag)
        {
            Type pageType = navItemTag == "settings" ? typeof(SettingsPage) : _pages.GetValueOrDefault(navItemTag);
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type? preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(pageType is null) && !Type.Equals(preNavPageType, pageType))
            {
                ContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
            }
        }

        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args is
                {
                    EventType: CoreAcceleratorKeyEventType.SystemKeyDown,
                    VirtualKey: VirtualKey.Left,
                    KeyStatus.IsMenuKeyDown: true,
                    Handled: false
                })
            {
                args.Handled = TryGoBack();
            }
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        private void NavView_BackRequested(muxc.NavigationView sender, muxc.NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs e)
        {
            // Handle mouse back button.
            if (e.CurrentPoint.Properties.IsXButton1Pressed)
            {
                e.Handled = TryGoBack();
            }
        }

        private bool TryGoBack()
        {
            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                NavView.DisplayMode is muxc.NavigationViewDisplayMode.Compact or muxc.NavigationViewDisplayMode.Minimal)
                NavView.IsPaneOpen = false;

            if (ViewModel.PlayerVisible && PlayerFrame.Content is PlayerPage { ViewModel: { } vm })
            {
                vm.GoBack();
                return true;
            }

            if (ContentFrame.Content is IContentFrame { CanGoBack: true } page)
            {
                page.GoBack();
                return true;
            }

            if (!ContentFrame.CanGoBack)
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (muxc.NavigationViewItem)NavView.SettingsItem;
            }
            else if (ContentFrame.SourcePageType != null)
            {
                muxc.NavigationViewItem? selectedItem = GetNavigationItemForPageType(e.SourcePageType);

                if (selectedItem == null && ViewModel.TryGetPageTypeFromParameter(e.Parameter, out Type pageType))
                {
                    selectedItem = GetNavigationItemForPageType(pageType);
                }

                NavView.SelectedItem = selectedItem;
            }
        }

        private muxc.NavigationViewItem? GetNavigationItemForPageType(Type pageType)
        {
            KeyValuePair<string, Type> item = _pages.FirstOrDefault(p => p.Value == pageType);

            muxc.NavigationViewItem? selectedItem = NavView.MenuItems
                .OfType<muxc.NavigationViewItem>()
                .FirstOrDefault(n => n.Tag.Equals(item.Key));

            return selectedItem;
        }

        private void NavView_OnDisplayModeChanged(muxc.NavigationView sender, muxc.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateNavigationViewState(args.DisplayMode, NavView.IsPaneOpen);
            ViewModel.NavigationViewDisplayMode = (NavigationViewDisplayMode)args.DisplayMode;
        }

        private void UpdateNavigationViewState(muxc.NavigationViewDisplayMode displayMode, bool paneOpen)
        {
            if (ViewModel.PlayerVisible)
            {
                VisualStateManager.GoToState(this, "Hidden", true);
                return;
            }

            switch (displayMode)
            {
                case muxc.NavigationViewDisplayMode.Minimal:
                    VisualStateManager.GoToState(this, "Minimal", true);
                    break;
                case muxc.NavigationViewDisplayMode.Expanded when paneOpen:
                    VisualStateManager.GoToState(this, "Expanded", true);
                    break;
                case muxc.NavigationViewDisplayMode.Expanded:
                case muxc.NavigationViewDisplayMode.Compact:
                    VisualStateManager.GoToState(this, "Compact", true);
                    break;
            }
        }

        private void NavView_OnPaneOpening(muxc.NavigationView sender, object args)
        {
            UpdateNavigationViewState(sender.DisplayMode, sender.IsPaneOpen);
        }

        private void NavView_OnPaneClosing(muxc.NavigationView sender, object args)
        {
            UpdateNavigationViewState(sender.DisplayMode, sender.IsPaneOpen);
        }

        private void NavViewSearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdateSearchSuggestions(sender.Text);
            }
        }

        private void NavViewSearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Update the text box when navigating through the suggestion list using the keyboard.
            if (args.SelectedItem is SearchSuggestionItem suggestion)
            {
                // We set sender.Text directly instead of ViewModel.SearchQuery
                // to avoid triggering TextChanged event.
                sender.Text = suggestion.Name;
            }
        }

        private void NavViewSearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is SearchSuggestionItem suggestion)
            {
                ViewModel.SelectSuggestion(suggestion);
            }
            else
            {
                ViewModel.SubmitSearch(args.QueryText);
            }

            ViewModel.SearchQuery = string.Empty;
            ViewModel.SearchSuggestions.Clear();
            if (ViewModel.NavigationViewDisplayMode != NavigationViewDisplayMode.Expanded)
            {
                ViewModel.IsPaneOpen = false;
            }
        }

        /// <summary>
        /// Give the <see cref="NavViewSearchBox"/> text entry box focus ("Focused" visual state) through the keyboard shortcut combination.
        /// </summary>
        private void NavViewSearchBoxKeyboardAcceleratorFocus_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            NavViewSearchBox.Focus(FocusState.Keyboard);
            args.Handled = true;
        }

        /// <summary>
        /// Give the <see cref="NavViewSearchBox"/> text entry box focus ("Focused" visual state) through the access key combination.
        /// </summary
        private void NavViewSearchBox_OnAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
        {
            NavViewSearchBox.Focus(FocusState.Keyboard);
            args.Handled = true;
        }

        private void NavView_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Link;
            if (e.DragUIOverride != null) e.DragUIOverride.Caption = Strings.Resources.Play;
        }

        private void NavView_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            ViewModel.OnDrop(e.DataView);
        }
    }
}
