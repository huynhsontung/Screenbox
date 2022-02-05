using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ModernVLC.Core;
using ModernVLC.ViewModels;
using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ModernVLC.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        private readonly VirtualKey PeriodKey = (VirtualKey)190;
        private readonly VirtualKey CommaKey = (VirtualKey)188;

        internal PlayerViewModel ViewModel => (PlayerViewModel)DataContext;

        public PlayerPage()
        {
            DataContext = App.Services.GetRequiredService<PlayerViewModel>();
            this.InitializeComponent();
            RegisterEventHandlers();
            ConfigureTitleBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.ToBeOpened = e.Parameter;
        }

        private void RegisterEventHandlers()
        {
            PointerEventHandler pointerPressedEventHandler = (s, e) => ViewModel.ShouldUpdateTime = false;
            PointerEventHandler pointerReleasedEventHandler = (s, e) => ViewModel.ShouldUpdateTime = true;
            SeekBar.AddHandler(PointerPressedEvent, pointerPressedEventHandler, true);
            SeekBar.AddHandler(PointerReleasedEvent, pointerReleasedEventHandler, true);
            SeekBar.AddHandler(PointerCanceledEvent, pointerReleasedEventHandler, true);
        }

        private void ConfigureTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
            var coreApp = CoreApplication.GetCurrentView();
            coreApp.TitleBar.ExtendViewIntoTitleBar = true;

            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            view.TitleBar.InactiveBackgroundColor = Windows.UI.Colors.Transparent;
            view.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
        }

        private void PlaybackSpeedItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (RadioMenuFlyoutItem)sender;
            var speedText = item.Text;
            float.TryParse(speedText, out var speed);
            ViewModel.SetPlaybackSpeed(speed);
        }

        private Symbol GetPlayPauseSymbol(bool isPlaying) => isPlaying ? Symbol.Pause : Symbol.Play;

        private Symbol GetMuteToggleSymbol(bool isMute) => isMute ? Symbol.Mute : Symbol.Volume;

        private Symbol GetFullscreenToggleSymbol(bool isFullscreen) => isFullscreen ? Symbol.BackToWindow : Symbol.FullScreen;

        private Visibility GetBufferingVisibilityIndicator(VLCState state) =>
            state == VLCState.Buffering || state == VLCState.Opening ? Visibility.Visible : Visibility.Collapsed;

        private InfoBarSeverity ConvertInfoBarSeverity(NotificationLevel level)
        {
            switch (level)
            {
                case NotificationLevel.Error:
                    return InfoBarSeverity.Error;
                case NotificationLevel.Warning:
                    return InfoBarSeverity.Warning;
                default:
                    return InfoBarSeverity.Informational;
            }
        }

        private void ProcessVideoViewKeyboardAccelerators(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) =>
            ViewModel.ProcessKeyboardAccelerators(sender, args);

        private void Flyout_Opened(object sender, object e) => ViewModel.FlyoutOpened = true;

        private void Flyout_Closed(object sender, object e) => ViewModel.FlyoutOpened = false;

        private void VideoView_Tapped(object sender, TappedRoutedEventArgs e) => VideoView.Focus(FocusState.Programmatic);
    }
}
