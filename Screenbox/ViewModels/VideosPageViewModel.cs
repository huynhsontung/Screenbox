#nullable enable

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.ViewModels
{
    internal partial class VideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _urlText;

        public List<MediaViewModel> Videos { get; private set; }

        private readonly IFilesService _filesService;

        public VideosPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _urlText = string.Empty;
            Videos = new(0);
        }

        public async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var videos = await _filesService.LoadVideosFromLibraryAsync();
            Videos = videos;
        }

        public void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UrlText))
                Messenger.Send(new PlayMediaMessage(UrlText));
        }

        public async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            StorageFile? pickedFile = await _filesService.PickFileAsync();

            if (pickedFile != null)
                Messenger.Send(new PlayMediaMessage(pickedFile));
        }

        public void VideosItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MediaViewModel item)
            {
                Messenger.Send(new PlayMediaMessage(item));
            }
        }
    }
}
