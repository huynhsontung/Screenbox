#nullable enable

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
using Screenbox.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.ViewModels
{
    internal partial class VideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string _urlText;

        [ObservableProperty] private string _titleText;

        [ObservableProperty] private bool _canGoBack;

        private readonly IFilesService _filesService;

        public VideosPageViewModel(IFilesService filesService)
        {
            _filesService = filesService;
            _urlText = string.Empty;
            _titleText = Strings.Resources.Videos;
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
    }
}
