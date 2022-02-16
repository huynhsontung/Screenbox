using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
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

        internal PlayerViewModel ViewModel => (PlayerViewModel)DataContext;

        private readonly IServiceScope _scope; 

        public PlayerPage()
        {
            _scope = App.Services.CreateScope();
            DataContext = _scope.ServiceProvider.GetRequiredService<PlayerViewModel>();
            this.InitializeComponent();
            RegisterPointerHandlersForSeekBar();
            ConfigureTitleBar();
        }

        public void FocusVideoView()
        {
            FocusButton.Focus(FocusState.Programmatic);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.ToBeOpened = e.Parameter;
            FocusVideoView();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _scope.Dispose();
        }

        private void RegisterPointerHandlersForSeekBar()
        {
            void PointerPressedEventHandler(object s, PointerRoutedEventArgs e) => ViewModel.ShouldUpdateTime = false;
            void PointerReleasedEventHandler(object s, PointerRoutedEventArgs e) => ViewModel.ShouldUpdateTime = true;
            SeekBar.AddHandler(PointerPressedEvent, (PointerEventHandler)PointerPressedEventHandler, true);
            SeekBar.AddHandler(PointerReleasedEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
            SeekBar.AddHandler(PointerCanceledEvent, (PointerEventHandler)PointerReleasedEventHandler, true);
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

        private Symbol GetPlayPauseSymbol(bool isPlaying) => isPlaying ? Symbol.Pause : Symbol.Play;

        private Symbol GetMuteToggleSymbol(bool isMute) => isMute ? Symbol.Mute : Symbol.Volume;

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
                default:
                    return InfoBarSeverity.Informational;
            }
        }

        private string GetHeightAsVec3(Size viewSize) => $"0,{viewSize.Height},0";
    }
}
