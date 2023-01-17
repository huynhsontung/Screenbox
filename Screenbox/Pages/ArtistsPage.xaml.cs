using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistsPage : Page
    {
        internal MusicPageViewModel ViewModel => (MusicPageViewModel)DataContext;

        internal CommonViewModel Common { get; }

        public ArtistsPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<MusicPageViewModel>();
            Common = App.Services.GetRequiredService<CommonViewModel>();
        }

        private void ArtistGridView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // TODO: Load artist image from the internet
        }
    }
}
