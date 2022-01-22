using LibVLCSharp.Platforms.UWP;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Microsoft.UI.Xaml.Controls;
using ModernVLC.Services;
using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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

        private void VideoView_Initialized(object sender, InitializedEventArgs e) => ViewModel.Initialize(e.SwapChainOptions);

        private void VideoView_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
        {
            long seekAmount = 0;
            int volumeChange = 0;
            int direction = 0;

            switch (args.Key)
            {
                case VirtualKey.Left:
                    direction = -1;
                    break;
                case VirtualKey.Right:
                    direction = 1;
                    break;
                case VirtualKey.Up:
                    volumeChange = 10;
                    break;
                case VirtualKey.Down:
                    volumeChange = -10;
                    break;
                case (VirtualKey)190 when args.Modifiers == VirtualKeyModifiers.None:   // Period (".")
                    ViewModel.JumpFrame(false);
                    return;
                case (VirtualKey)188 when args.Modifiers == VirtualKeyModifiers.None:   // Comma (",")
                    ViewModel.JumpFrame(true);
                    return;
            }

            switch (args.Modifiers)
            {
                case VirtualKeyModifiers.Control:
                    seekAmount = 10000;
                    break;
                case VirtualKeyModifiers.Shift:
                    seekAmount = 1000;
                    break;
                case VirtualKeyModifiers.None:
                    seekAmount = 5000;
                    break;
            }

            if (seekAmount * direction != 0)
            {
                ViewModel.SeekCommand.Execute(seekAmount * direction);
            }

            if (volumeChange != 0)
            {
                ViewModel.ChangeVolumeCommand.Execute(volumeChange);
            }
        }

        private void AudioTrack_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems[0] == null) return;
            var selected = (TrackDescription)args.AddedItems[0];
            ViewModel.SetAudioTrackCommand.Execute(selected.Id);
        }

        private void Subtitles_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems[0] == null) return;
            var selected = (TrackDescription)args.AddedItems[0];
            ViewModel.SetSubtitleCommand.Execute(selected.Id);
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

        private void VideoView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "Open";
        }

        private async void VideoView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageItem = items[0] as StorageFile;
                    var extension = storageItem.FileType;
                    if (FileService.SupportedFormats.Contains(extension))
                    {
                        ViewModel.OpenCommand.Execute(storageItem.Path);
                    }
                }
            }

            if (e.DataView.Contains(StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                if (uri.IsFile)
                {
                    ViewModel.OpenCommand.Execute(uri);
                }
            }
        }
    }
}
