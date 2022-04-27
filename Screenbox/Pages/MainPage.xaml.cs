#nullable enable

using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Services;
using Screenbox.ViewModels;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page
    {
        private PlayerPageViewModel ViewModel => (PlayerPageViewModel)DataContext;

        private StorageFile? _pickedFile;
        private readonly IFilesService _filesService;

        public MainPage()
        {
            _filesService = App.Services.GetRequiredService<IFilesService>();
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var videos = await _filesService.LoadVideosFromLibraryAsync();
            Videos.ItemsSource = videos;
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UrlBox.Text))
                ViewModel.OpenCommand.Execute(UrlBox.Text);
        }

        private async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            _pickedFile = await _filesService.PickFileAsync();
            
            if (_pickedFile != null)
                ViewModel.OpenCommand.Execute(_pickedFile);
        }

        private void VideosItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VideoViewModel item)
            {
                ViewModel.OpenCommand.Execute(item.OriginalFile);
            }
        }
    }
}
