#nullable enable

using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Controls;
using Screenbox.Services;
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

        private readonly SystemMediaTransportControlsViewModel _systemMediaTransportControlsViewModel;  // unused. just for holding reference

        public PlayerPage()
        {
            DataContext = App.Services.GetRequiredService<PlayerPageViewModel>();
            _systemMediaTransportControlsViewModel = App.Services.GetRequiredService<SystemMediaTransportControlsViewModel>();
            this.InitializeComponent();
            RegisterSeekBarPointerHandlers();
            FocusVideoViewOnEvents();
            SetTitleBar();

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        public void FocusVideoView()
        {
            VideoView.Focus(FocusState.Programmatic);
        }

        public void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                ViewModel.RequestPlay(e.Parameter);
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void FocusVideoViewOnEvents()
        {
            Loaded += (_, _) => FocusVideoView();
            LayoutGroup.CurrentStateChanged += (_, args) =>
            {
                if (args.OldState?.Name == "Mini" && (args.NewState == null || args.NewState.Name == "Normal"))
                    FocusVideoView();
            };
        }

        private void RegisterSeekBarPointerHandlers()
        {
            SeekBar? seekBar = PlayerControls.FindDescendant<SeekBar>();
            Guard.IsNotNull(seekBar, nameof(seekBar));
            seekBar.AddHandler(PointerPressedEvent, (PointerEventHandler)SeekBarPointerPressedEventHandler, true);
            seekBar.AddHandler(PointerReleasedEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
            seekBar.AddHandler(PointerCanceledEvent, (PointerEventHandler)SeekBarPointerReleasedEventHandler, true);
        }

        private void SeekBarPointerPressedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerPressed = true;
        }

        private void SeekBarPointerReleasedEventHandler(object s, PointerRoutedEventArgs e)
        {
            ViewModel.SeekBarPointerPressed = false;
            FocusVideoView();
        }

        private void VideoView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PlayerControls.ViewModel.ToggleFullscreenCommand.Execute(null);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerPageViewModel.ViewMode))
            {
                switch (ViewModel.ViewMode)
                {
                    case WindowViewMode.Default:
                        VisualStateManager.GoToState(this, "Normal", true);
                        break;
                    case WindowViewMode.Compact:
                        VisualStateManager.GoToState(this, "Compact", true);
                        break;
                    case WindowViewMode.FullScreen:
                        VisualStateManager.GoToState(this, "Fullscreen", true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
