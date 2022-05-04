#nullable enable

using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Screenbox.Core.Messages;
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
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleBar();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            var videos = await _filesService.LoadVideosFromLibraryAsync();
            Videos.ItemsSource = videos;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlayerHidden) && ViewModel.PlayerHidden)
            {
                SetTitleBar();
            }
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UrlBox.Text))
                WeakReferenceMessenger.Default.Send(new PlayMediaMessage(UrlBox.Text));
        }

        private async void PickFileButtonClick(object sender, RoutedEventArgs e)
        {
            _pickedFile = await _filesService.PickFileAsync();
            
            if (_pickedFile != null)
                WeakReferenceMessenger.Default.Send(new PlayMediaMessage(_pickedFile));
        }

        private void VideosItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MediaViewModel item)
            {
                WeakReferenceMessenger.Default.Send(new PlayMediaMessage(item));
            }
        }
    }
}
