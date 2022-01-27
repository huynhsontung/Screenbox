using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.UI.Xaml.Controls;
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
        public PlayerPage()
        {
            this.InitializeComponent();
            RegisterEventHandlers();
            ConfigureTitleBar();
        }

        public void Open(object target) => ViewModel.OpenCommand.Execute(target);

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.ToBeOpened = e.Parameter;
        }

        private void RegisterEventHandlers()
        {
            PointerEventHandler pointerPressedEventHandler = (s, e) => ViewModel.SetInteracting(true);
            PointerEventHandler pointerReleasedEventHandler = (s, e) => ViewModel.SetInteracting(false);
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
            ViewModel.SetPlaybackSpeedCommand.Execute(speed);
        }

        private Symbol GetPlayPauseSymbol(bool isPlaying) => isPlaying ? Symbol.Pause : Symbol.Play;

        private Symbol GetMuteToggleSymbol(bool isMute) => isMute ? Symbol.Mute : Symbol.Volume;

        private Symbol GetFullscreenToggleSymbol(bool isFullscreen) => isFullscreen ? Symbol.BackToWindow : Symbol.FullScreen;

        private Visibility GetBufferingVisibilityIndicator(VLCState state) =>
            state == VLCState.Buffering || state == VLCState.Opening ? Visibility.Visible : Visibility.Collapsed;
    }
}
