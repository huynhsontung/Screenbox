using ModernVLC.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel
    {
        private bool _verticalLock;
        private double _timeBeforeManipulation;

        private void ConfigureVideoViewManipulation()
        {
            VideoView.ManipulationStarted += new ManipulationStartedEventHandler(VideoView_ManipulationStarted);
            VideoView.ManipulationDelta += new ManipulationDeltaEventHandler(VideoView_ManipulationDelta);
            VideoView.ManipulationCompleted += new ManipulationCompletedEventHandler(VideoView_ManipulationCompleted);

            VideoView.ManipulationMode =
                ManipulationModes.TranslateX |
                ManipulationModes.TranslateY;
        }

        private void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _verticalLock = false;
            StatusMessage = null;
            MediaPlayer.ShouldUpdateTime = true;
        }

        private void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var actualWidth = VideoView.ActualWidth;
            var actualHeight = VideoView.ActualHeight;

            var horizontalScaler = e.Delta.Translation.X / actualWidth;
            var verticalScaler = e.Delta.Translation.Y / actualHeight;
            if (_verticalLock || Math.Abs(verticalScaler) > 0.01)
            {
                _verticalLock = true;
                var volumeChange = (int)(-verticalScaler * 100);
                MediaPlayer.ObservableVolume += volumeChange;
                StatusMessage = $"Volume {MediaPlayer.ObservableVolume}%";
            }
            else if (MediaPlayer.IsSeekable)
            {
                var maxChange = Math.Max(MediaPlayer.Length * 0.1, 5000);
                var timeChange = horizontalScaler * maxChange;
                MediaPlayer.ObservableTime += timeChange;

                var changeText = HumanizedDurationConverter.Convert(MediaPlayer.ObservableTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                StatusMessage = $"{HumanizedDurationConverter.Convert(MediaPlayer.ObservableTime)} ({changeText})";
            }
        }

        private void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            MediaPlayer.ShouldUpdateTime = false;
            _timeBeforeManipulation = MediaPlayer.Time;
        }
    }
}
