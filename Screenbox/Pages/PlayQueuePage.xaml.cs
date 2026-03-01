#nullable enable

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls;
using Screenbox.Core.Enums;
using Screenbox.Core.Services;
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

        private readonly INotificationService _notificationService;

        public PlayQueuePage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PlayQueuePageViewModel>();
            Common = Ioc.Default.GetRequiredService<CommonViewModel>();
            _notificationService = Ioc.Default.GetRequiredService<INotificationService>();
        }

        private async void PlayQueuePage_OnLoaded(object sender, RoutedEventArgs e)
        {
            await PlayQueue.SmoothScrollActiveItemIntoViewAsync();
        }

        private async void HeaderSaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            string? playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                await PlayQueue.ViewModel.SaveToNewPlaylistAsync(playlistName!);
                _notificationService.RaiseNotification(NotificationLevel.Success,
                    Strings.Resources.PlaylistCreatedNotificationTitle, playlistName!);
            }
        }
    }
}
