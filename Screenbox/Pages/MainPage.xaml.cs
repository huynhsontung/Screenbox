#nullable enable

using Screenbox.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.UI;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page
    {
        private MainPageViewModel ViewModel => (MainPageViewModel)DataContext;

        private readonly Dictionary<string, Type> _pages;
        private readonly Frame _playerFrame;
        private Border? _contentRootBorder;

        public MainPage()
        {
            InitializeComponent();
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            _playerFrame = CreatePlayerFrame();
            _pages = new Dictionary<string, Type>
            {
                { "home", typeof(HomePage) },
                { "videos", typeof(VideosPage) },
                { "music", typeof(MusicPage) },
                { "queue", typeof(PlayQueuePage) },
                { "settings", typeof(SettingsPage) }
            };

            DataContext = App.Services.GetRequiredService<MainPageViewModel>();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                ViewModel.PlayerVisible = true;
            }
        }

        public void NavigateContentFrame(string navigationTag)
        {
            ViewModel.PlayerVisible = false;
            NavView_Navigate(navigationTag, new CommonNavigationTransitionInfo());
        }

        private static Frame CreatePlayerFrame()
        {
            /*
             * Due to the current LibVLC limitation, there can only be one instance of
             * the media player element per LibVLC instance. This means the player will
             * break if you navigate away from the PlayerPage.
             * This limitation will go away with LibVLC 4.x.
             */

            Frame playerFrame = new();
            playerFrame.SetValue(Grid.RowProperty, 0);
            playerFrame.SetValue(Grid.RowSpanProperty, 3);
            playerFrame.SetValue(Grid.ColumnProperty, 0);
            playerFrame.SetValue(Grid.ColumnSpanProperty, 2);
            playerFrame.Navigate(typeof(PlayerPage));
            return playerFrame;
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
            if (ApplicationView.GetForCurrentView()?.TitleBar is { } titleBar)
            {
                titleBar.ButtonForegroundColor = null;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleBar();
            if (NavView.FindDescendant("ContentRoot") is Grid contentRoot)
            {
                contentRoot.Children.Add(_playerFrame);
                _contentRootBorder = contentRoot.FindChild<Border>();
            }

            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            NavView.SelectedItem = NavView.MenuItems[0];
            ViewModel.NavigationViewDisplayMode = NavView.DisplayMode;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlayerVisible))
            {
                if (_contentRootBorder != null)
                {
                    _contentRootBorder.Visibility = ViewModel.PlayerVisible ? Visibility.Collapsed : Visibility.Visible;
                }

                // Do not try to implement the following using XAML Behaviors APIs and VisualStates
                // Will introduce visual artifacts with NavView
                if (ViewModel.PlayerVisible)
                {
                    NavView.IsPaneVisible = false;
                    NavView.IsPaneOpen = false;
                    NavView.AlwaysShowHeader = false;
                    ContentFrame.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SetTitleBar();
                    NavView.IsPaneVisible = true;
                    NavView.AlwaysShowHeader = true;
                    ContentFrame.Visibility = Visibility.Visible;
                }

                UpdateTitleBarState();
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void NavView_SelectionChanged(muxc.NavigationView sender, muxc.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo transitionInfo)
        {
            Type pageType = navItemTag == "settings" ? typeof(SettingsPage) : _pages.GetValueOrDefault(navItemTag);
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type? preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(pageType is null) && !Type.Equals(preNavPageType, pageType))
            {
                ContentFrame.Navigate(pageType, null, transitionInfo);
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
                (NavView.DisplayMode == muxc.NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal))
                return false;

            if (ContentFrame.Content is ContentPage page && page.CanGoBack)
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
                NavView.Header = Strings.Resources.Settings;
            }
            else if (ContentFrame.SourcePageType != null)
            {
                KeyValuePair<string, Type> item = _pages.FirstOrDefault(p => p.Value == e.SourcePageType);

                muxc.NavigationViewItem selectedItem = NavView.MenuItems
                    .OfType<muxc.NavigationViewItem>()
                    .First(n => n.Tag.Equals(item.Key));
                
                NavView.SelectedItem = selectedItem;
                NavView.Header = ContentFrame.Content is ContentPage page
                    ? page.Header
                    : selectedItem.Content?.ToString();
            }
        }

        private void NavView_OnDisplayModeChanged(muxc.NavigationView sender, muxc.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateTitleBarState();
            ViewModel.NavigationViewDisplayMode = args.DisplayMode;
        }

        private void UpdateTitleBarState()
        {
            if (ViewModel.PlayerVisible)
            {
                VisualStateManager.GoToState(this, "Hidden", true);
                if (NavView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal)
                {
                    VisualStateManager.GoToState(NavView, "HeaderCollapsed", false);
                }
                return;
            }

            switch (NavView.DisplayMode)
            {
                case muxc.NavigationViewDisplayMode.Minimal:
                    VisualStateManager.GoToState(this, "Minimal", true);
                    break;
                case muxc.NavigationViewDisplayMode.Expanded when NavView.IsPaneOpen:
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
            if (TitleBarGroup.CurrentState?.Name == "Hidden") return;
            if (sender.DisplayMode == muxc.NavigationViewDisplayMode.Expanded)
                VisualStateManager.GoToState(this, "Expanded", true);
        }

        private void NavView_OnPaneClosing(muxc.NavigationView sender, object args)
        {
            if (TitleBarGroup.CurrentState?.Name == "Hidden") return;
            if (sender.DisplayMode == muxc.NavigationViewDisplayMode.Expanded)
                VisualStateManager.GoToState(this, "Compact", true);
        }
    }
}
