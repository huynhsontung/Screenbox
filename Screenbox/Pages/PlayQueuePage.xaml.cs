using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Core.ViewModels;

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
            DataContext = App.Services.GetRequiredService<PlayQueuePageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        private async void PlayQueuePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await PlaylistView.SmoothScrollActiveItemIntoViewAsync();
        }
    }
}
