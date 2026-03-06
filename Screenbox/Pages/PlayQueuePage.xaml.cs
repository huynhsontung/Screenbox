#nullable enable

using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

        /// <summary>
        /// Code-behind command for adding a folder's media files to the play queue.
        /// Catches errors and sends a localized error notification via the view model.
        /// </summary>
        public IAsyncRelayCommand AddFolderCommand { get; }

        public PlayQueuePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayQueuePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();

            AddFolderCommand = new AsyncRelayCommand(AddFolderExecuteAsync);
        }

        private async System.Threading.Tasks.Task AddFolderExecuteAsync()
        {
            try
            {
                await ViewModel.AddFolderAsync();
            }
            catch (Exception e)
            {
                ViewModel.SendErrorMessage(Screenbox.Strings.Resources.FailedToOpenFilesNotificationTitle, e.Message);
            }
        }

        private async void PlayQueuePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await PlayQueue.SmoothScrollActiveItemIntoViewAsync();
        }
    }
}
