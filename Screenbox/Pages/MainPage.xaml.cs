#nullable enable

using Screenbox.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
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
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            NotificationView.Translation = new Vector3(0, 0, 8);

            _pages = new Dictionary<string, Type>
            {
                { "home", typeof(HomePage) },
                { "videos", typeof(VideosPage) },
                { "music", typeof(MusicPage) },
                { "queue", typeof(PlayQueuePage) },
                { "network", typeof(NetworkPage) },
                { "settings", typeof(SettingsPage) }
            };

            DataContext = App.Services.GetRequiredService<MainPageViewModel>();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            if (ApplicationView.GetForCurrentView()?.TitleBar is { } titleBar)
            {
                titleBar.ButtonForegroundColor = null;
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            ViewModel.NavigationViewDisplayMode = NavView.DisplayMode;
            if (!ViewModel.PlayerVisible)
            {
                SetTitleBar();
                NavView.SelectedItem = NavView.MenuItems[0];
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
                    }
                }

                UpdateNavigationViewState(NavView.DisplayMode, NavView.IsPaneOpen);
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
                return false;

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
                KeyValuePair<string, Type> item = _pages.FirstOrDefault(p => p.Value == e.SourcePageType);

                muxc.NavigationViewItem? selectedItem = NavView.MenuItems
                    .OfType<muxc.NavigationViewItem>()
                    .FirstOrDefault(n => n.Tag.Equals(item.Key));
                
                NavView.SelectedItem = selectedItem;
            }
        }

        private void NavView_OnDisplayModeChanged(muxc.NavigationView sender, muxc.NavigationViewDisplayModeChangedEventArgs args)
        {
            UpdateNavigationViewState(args.DisplayMode, NavView.IsPaneOpen);
            ViewModel.NavigationViewDisplayMode = args.DisplayMode;
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
    }
}
