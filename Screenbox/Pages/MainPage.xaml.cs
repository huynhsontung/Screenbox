using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.Services;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFile _pickedFile;
        private readonly IFilesService _filesService;
        private readonly IPlaylistService _playlistService;

        public MainPage()
        {
            _filesService = App.Services.GetRequiredService<IFilesService>();
            _playlistService = App.Services.GetRequiredService<IPlaylistService>();
            this.InitializeComponent();
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            if (_pickedFile != null)
            {
                _playlistService.RequestOpen(_pickedFile);
            }

            if (!string.IsNullOrEmpty(UrlBox.Text))
            {
                _playlistService.RequestOpen(UrlBox.Text);
            }
        }

        private async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            _pickedFile = await _filesService.PickFileAsync();
            
            if (_pickedFile != null)
                FileNameText.Text = _pickedFile.Name;
        }
    }
}
