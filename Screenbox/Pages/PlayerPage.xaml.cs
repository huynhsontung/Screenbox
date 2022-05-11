using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
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

            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            PlaylistViewModel.PropertyChanged += PlaylistViewModelOnPropertyChanged;
        }

        public void FocusVideoView()
        {
            VideoView.Focus(FocusState.Programmatic);
        }

        public void SetPreviousNextButtonVisibility()
        {
            if (ViewModel.IsCompact) return;
            VisualStateManager.GoToState(this,
                PlaylistViewModel.CanSkip ? "PreviousNextVisible" : "PreviousNextHidden", true);
        }

        public void SetTitleBar()
        {
            Window.Current.SetTitleBar(TitleBarElement);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                WeakReferenceMessenger.Default.Send(new PlayMediaMessage(e.Parameter));
            }
        }

        private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaylistViewModel.CanSkip))
            {
                SetPreviousNextButtonVisibility();
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

        private string GetRepeatModeGlyph(RepeatMode repeatMode)
        {
            switch (repeatMode)
            {
                case RepeatMode.Off:
                    return "\uf5e7";
                case RepeatMode.All:
                    return "\ue8ee";
                case RepeatMode.One:
                    return "\ue8ed";
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatMode), repeatMode, null);
            }
        }

        private string GetHeightAsVec3(Size viewSize) => $"0,{viewSize.Height},0";
    }
}
