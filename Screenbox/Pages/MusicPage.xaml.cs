#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicPage : Page
    {
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        public MusicPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            VisualStateManager.GoToState(this, "Fetching", true);
            await ViewModel.FetchSongsAsync();
            VisualStateManager.GoToState(this, ViewModel.GroupedSongs?.Count > 0 ? "Normal" : "NoContent", true);
        }
    }
}
