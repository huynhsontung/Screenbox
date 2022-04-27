#nullable enable

using System;
using System.Windows.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Diagnostics;
using Screenbox.Services;
using Screenbox.ViewModels;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page
    {
        public ICommand? OpenCommand { get; set; }

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
            Guard.IsNotNull(OpenCommand, nameof(OpenCommand));
            if (!string.IsNullOrWhiteSpace(UrlBox.Text))
                OpenCommand.Execute(UrlBox.Text);
        }

        private async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            Guard.IsNotNull(OpenCommand, nameof(OpenCommand));
            _pickedFile = await _filesService.PickFileAsync();
            
            if (_pickedFile != null)
                OpenCommand.Execute(_pickedFile);
        }

        private void VideosItemClick(object sender, ItemClickEventArgs e)
        {
            Guard.IsNotNull(OpenCommand, nameof(OpenCommand));
            if (e.ClickedItem is VideoViewModel item)
            {
                OpenCommand.Execute(item.OriginalFile);
            }
        }
    }
}
