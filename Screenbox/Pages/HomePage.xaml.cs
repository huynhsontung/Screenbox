﻿#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        internal HomePageViewModel ViewModel => (HomePageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public HomePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<HomePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            VisualStateManager.GoToState(this, ViewModel.HasRecentMedia ? "RecentMedia" : "Welcome", false);
        }
    }
}
