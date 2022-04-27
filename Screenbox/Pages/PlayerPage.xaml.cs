using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Screenbox.Core;
using Screenbox.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Screenbox.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        private const VirtualKey PeriodKey = (VirtualKey)190;
        private const VirtualKey CommaKey = (VirtualKey)188;

        internal PlayerPageViewModel ViewModel => (PlayerPageViewModel)DataContext;

        public PlayerPage()
        {
            DataContext = App.Services.GetRequiredService<PlayerPageViewModel>();
            this.InitializeComponent();
            RegisterSeekBarPointerHandlers();
            FocusVideoViewOnEvents();
            Window.Current.SetTitleBar(TitleBarElement);
        }

        public void FocusVideoView()
        {
            VideoViewButton.Focus(FocusState.Programmatic);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.ToBeOpened = e.Parameter;
        }

        private void FocusVideoViewOnEvents()
        {
            VideoViewButton.Drop += (_, _) => FocusVideoView();
            Loaded += (_, _) => FocusVideoView();
            PageStates.CurrentStateChanged += (_, args) =>
            {
                if (args.NewState.Name == "PlayerVisible")
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
            var speedText = item.Text;
            float.TryParse(speedText, out var speed);
            ViewModel.SetPlaybackSpeed(speed);
        }

        private Symbol GetPlayPauseSymbol(bool isPlaying) => isPlaying ? Symbol.Pause : Symbol.Play;

        private Symbol GetFullscreenToggleSymbol(bool isFullscreen) => isFullscreen ? Symbol.BackToWindow : Symbol.FullScreen;

        private Visibility GetBufferingVisibilityIndicator(VLCState state) =>
            state is VLCState.Buffering or VLCState.Opening ? Visibility.Visible : Visibility.Collapsed;

        private InfoBarSeverity ConvertInfoBarSeverity(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return InfoBarSeverity.Error;
                case NotificationLevel.Warning:
                    return InfoBarSeverity.Warning;
                case NotificationLevel.Success:
                    return InfoBarSeverity.Success;
                default:
                    return InfoBarSeverity.Informational;
            }
        }

        private string GetHeightAsVec3(Size viewSize) => $"0,{viewSize.Height},0";
    }
}
