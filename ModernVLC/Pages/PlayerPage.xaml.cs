using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using ModernVLC.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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

        private void RegisterEventHandlers()
        {
            PointerEventHandler pointerPressedEventHandler = ViewModel.SeekBar_PointerPressed;
            PointerEventHandler pointerReleasedEventHandler = ViewModel.SeekBar_PointerReleased;
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
        }

        private void SeekBar_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //var position = e.GetCurrentPoint(SeekBar).Position;
            //var ratio = position.X / SeekBar.ActualWidth;
            //long potentialValue = (long)(ratio * SeekBar.Maximum);
            //SeekBarToolTip.Content = $"{potentialValue}";
            //SeekBarToolTip.HorizontalOffset = position.X;
        }

        public void FocusVideoView()
        {
            VideoView.Focus(FocusState.Programmatic);
        }
    }
}
