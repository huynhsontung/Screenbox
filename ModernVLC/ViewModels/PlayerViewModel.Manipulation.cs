using ModernVLC.Converters;
using System;
using Windows.UI.Xaml.Input;

namespace ModernVLC.ViewModels
{
    internal partial class PlayerViewModel
    {
        private enum ManipulationLock
        {
            None,
            Horizontal,
            Vertical
        }

        const double HorizontalChangePerPixel = 100;

        private ManipulationLock _lockDirection;
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
            StatusMessage = null;
            MediaPlayer.ShouldUpdateTime = true;
        }

        private void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var horizontalChange = e.Delta.Translation.X;
            var verticalChange = e.Delta.Translation.Y;
            var horizontalCumulative = e.Cumulative.Translation.X;
            var verticalCumulative = e.Cumulative.Translation.Y;
            if (Math.Abs(horizontalCumulative) < 50 && Math.Abs(verticalCumulative) < 50) return;

            if (_lockDirection == ManipulationLock.Vertical ||
                (_lockDirection == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50))
            {
                _lockDirection = ManipulationLock.Vertical;
                MediaPlayer.ObservableVolume += -verticalChange;
                StatusMessage = $"Volume {MediaPlayer.ObservableVolume:F0}%";
                return;
            }

            if (MediaPlayer.IsSeekable)
            {
                _lockDirection = ManipulationLock.Horizontal;
                MediaPlayer.ShouldUpdateTime = false;
                var timeChange = horizontalChange * HorizontalChangePerPixel;
                MediaPlayer.ObservableTime += timeChange;

                var changeText = HumanizedDurationConverter.Convert(MediaPlayer.ObservableTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                StatusMessage = $"{HumanizedDurationConverter.Convert(MediaPlayer.ObservableTime)} ({changeText})";
            }
        }

        private void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _lockDirection = ManipulationLock.None;
            _timeBeforeManipulation = MediaPlayer.Time;
        }
    }
}
