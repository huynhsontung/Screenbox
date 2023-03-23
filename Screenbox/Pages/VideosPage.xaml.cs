#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.Core.ViewModels;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideosPage : Page, IContentFrame
    {
        public Type ContentSourcePageType => ContentFrame.SourcePageType;

        public object? FrameContent => ContentFrame.Content;

        public bool CanGoBack => ContentFrame.CanGoBack;

        internal VideosPageViewModel ViewModel => (VideosPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        private readonly Dictionary<string, Type> _pages;

        public VideosPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<VideosPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();

            _pages = new Dictionary<string, Type>
            {
                { "folders", typeof(FolderViewPage) },
                { "all", typeof(AllVideosPage) }
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (Common.NavigationStates.TryGetValue(typeof(VideosPage), out string navigationState))
            {
                ContentFrame.SetNavigationState(navigationState);
                UpdateSelectedNavItem(ContentSourcePageType);
            }
            else
            {
                LibraryNavView.SelectedItem = LibraryNavView.MenuItems[0];
            }

            await ViewModel.FetchVideosAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Common.NavigationStates[typeof(VideosPage)] = ContentFrame.GetNavigationState();
            if (ContentFrame.Content is FolderViewPage page)
            {
                page.ViewModel.Clean();
            }
        }

        public void GoBack()
        {
            ContentFrame.GoBack(new SuppressNavigationTransitionInfo());
        }

        public void NavigateContent(Type pageType, object? parameter)
        {
            ContentFrame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
        }

        private void LibraryNavView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                string navItemTag = args.SelectedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag);
            }
        }

        private void NavView_Navigate(string navItemTag)
        {
            Type pageType = _pages.GetValueOrDefault(navItemTag);
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type? preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (pageType is not null && preNavPageType != pageType)
            {
                ContentFrame.Navigate(pageType, "VideosLibrary", new SuppressNavigationTransitionInfo());
            }
        }

        private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType != null)
            {
                UpdateSelectedNavItem(e.SourcePageType);
            }

            ViewModel.OnContentFrameNavigated(sender, e);
        }

        private void UpdateSelectedNavItem(Type sourcePageType)
        {
            KeyValuePair<string, Type> item = _pages.FirstOrDefault(p => p.Value == sourcePageType);

            Microsoft.UI.Xaml.Controls.NavigationViewItem? selectedItem = LibraryNavView.MenuItems
                .OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>()
                .FirstOrDefault(n => n.Tag.Equals(item.Key));

            LibraryNavView.SelectedItem = selectedItem;
        }

        private void BreadcrumbBar_OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            ViewModel.OnBreadcrumbBarItemClicked(args.Index);
        }
    }
}
