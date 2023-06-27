#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls;
using Screenbox.Core.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayQueuePage : Page
    {
        internal PlayQueuePageViewModel ViewModel => (PlayQueuePageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public PlayQueuePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayQueuePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
        }

        private async void PlayQueuePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await PlaylistView.SmoothScrollActiveItemIntoViewAsync();
        }

        private async void AddUrlMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Uri? uri = await OpenUrlDialog.GetUrlAsync();
            if (uri != null)
            {
                ViewModel.AddUrl(uri);
            }
        }
    }
}
