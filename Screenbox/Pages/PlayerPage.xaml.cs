using System.ComponentModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core.Messages;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        internal PlayerPageViewModel ViewModel => (PlayerPageViewModel)DataContext;

        internal PlaylistViewModel PlaylistViewModel { get; }

        public PlayerPage()
        {
            DataContext = App.Services.GetRequiredService<PlayerPageViewModel>();
            PlaylistViewModel = App.Services.GetRequiredService<PlaylistViewModel>();
            this.InitializeComponent();
            RegisterSeekBarPointerHandlers();
            FocusVideoViewOnEvents();
            SetTitleBar();

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        public void FocusVideoView()
        {
            VideoView.Focus(FocusState.Programmatic);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                WeakReferenceMessenger.Default.Send(new PlayMediaMessage(e.Parameter));
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.PlayerHidden) && !ViewModel.PlayerHidden)
            {
                SetTitleBar();
            }
        }

        private void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
        }

        private void FocusVideoViewOnEvents()
        {
            Loaded += (_, _) => FocusVideoView();
            PageStates.CurrentStateChanged += (_, args) =>
            {
                if (args.NewState == null || args.NewState.Name == "PlayerVisible")
                    FocusVideoView();
            };
        }

        private void RegisterSeekBarPointerHandlers()
        {
            void PointerReleasedEventHandler(object s, PointerRoutedEventArgs e)
            {
                FocusVideoView();
            }

            SeekBar.AddHandler(PointerReleasedEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
            SeekBar.AddHandler(PointerCanceledEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
        }

        private void PlaybackSpeedItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (RadioMenuFlyoutItem)sender;
            ViewModel.SetPlaybackSpeed(item.Text);
        }

        private string GetPlayPauseGlyph(bool isPlaying) => isPlaying ? "\uE103" : "\uE102";

        private string GetFullscreenToggleGlyph(bool isFullscreen) => isFullscreen ? "\uE1D8" : "\uE1D9";

        private string GetHeightAsVec3(Size viewSize) => $"0,{viewSize.Height},0";
    }
}
