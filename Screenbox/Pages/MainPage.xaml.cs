#nullable enable

using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Services;
using Screenbox.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Collections.Generic;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;

namespace Screenbox.Pages
{
    public sealed partial class MainPage : Page
    {
        private PlayerViewModel ViewModel => (PlayerViewModel)DataContext;

        private StorageFile? _pickedFile;
        private readonly IFilesService _filesService;

        private ObservableCollection<VideoViewModel> _videos = new();

        public MainPage()
        {
            _filesService = App.Services.GetRequiredService<IFilesService>();
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadVideosAsync(await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos));
            Videos.ItemsSource = _videos;
        }

        private async Task LoadVideosAsync(StorageLibrary library)
        {
            if (_videos.Count > 0)
            {
                _videos.Clear();
            }

            foreach (StorageFolder folder in library.Folders)
            {
                System.Diagnostics.Debug.WriteLine(folder.Name);
                QueryOptions options = new()
                {
                    FolderDepth = FolderDepth.Deep
                };

                options.FileTypeFilter.Add(".mp4");

                IReadOnlyList<StorageFile> files = await folder.CreateFileQueryWithOptions(options).GetFilesAsync();

                foreach (StorageFile file in files)
                {
                    System.Diagnostics.Debug.WriteLine(file.Name);
                    BitmapImage thumb = new();

                    await thumb.SetSourceAsync(await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView));

                    _videos.Add(new()
                    {
                        Title = file.DisplayName,
                        Duration = (await file.Properties.GetVideoPropertiesAsync()).Duration,
                        Thumbnail = thumb,
                        Location = file.Path.Replace("\\" + file.Name, ""),
                        OriginalFile = file
                    });
                }
            }
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
