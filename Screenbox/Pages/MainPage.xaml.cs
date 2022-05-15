#nullable enable

using Screenbox.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page
    {
        private PlayerPageViewModel ViewModel => (PlayerPageViewModel)DataContext;

        private readonly Dictionary<string, Type> _pages;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            _pages = new Dictionary<string, Type>
            {
                { "home", typeof(HomePage) },
                { "videos", typeof(VideosPage) },
                { "music", typeof(MusicPage) },
                { "settings", typeof(SettingsPage) }
            };
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleBar();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlayerHidden) && ViewModel.PlayerHidden)
            {
                SetTitleBar();
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
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == muxc.NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == muxc.NavigationViewDisplayMode.Minimal))
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
                NavView.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                var item = _pages.FirstOrDefault(p => p.Value == e.SourcePageType);

                NavView.SelectedItem = NavView.MenuItems
                    .OfType<muxc.NavigationViewItem>()
                    .First(n => n.Tag.Equals(item.Key));

                NavView.Header =
                    ((muxc.NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
            }
        }

        private void NavView_OnDisplayModeChanged(muxc.NavigationView sender, muxc.NavigationViewDisplayModeChangedEventArgs args)
        {
            switch (args.DisplayMode)
            {
                case muxc.NavigationViewDisplayMode.Minimal:
                    VisualStateManager.GoToState(this, "Minimal", true);
                    break;
                case muxc.NavigationViewDisplayMode.Expanded when sender.IsPaneOpen:
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
            if (sender.DisplayMode == muxc.NavigationViewDisplayMode.Expanded)
                VisualStateManager.GoToState(this, "Expanded", true);
        }

        private void NavView_OnPaneClosing(muxc.NavigationView sender, object args)
        {
            if (sender.DisplayMode == muxc.NavigationViewDisplayMode.Expanded)
                VisualStateManager.GoToState(this, "Compact", true);
        }
    }
}
