using System;
using Windows.UI.Xaml.Input;
using Screenbox.Converters;

namespace Screenbox.ViewModels
{
    internal partial class PlayerViewModel
    {
        private enum ManipulationLock
        {
            None,
            Horizontal,
            Vertical
        }

        const double HorizontalChangePerPixel = 200;

        private ManipulationLock _lockDirection;
        private double _timeBeforeManipulation;

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_lockDirection == ManipulationLock.None) return;
            OverrideVisibilityChange(100);
            StatusMessage = null;
            if (MediaPlayer != null) MediaPlayer.ShouldUpdateTime = true;
        }

        public void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (MediaPlayer == null) return;
            var horizontalChange = e.Delta.Translation.X;
            var verticalChange = e.Delta.Translation.Y;
            var horizontalCumulative = e.Cumulative.Translation.X;
            var verticalCumulative = e.Cumulative.Translation.Y;
            if (Math.Abs(horizontalCumulative) < 50 && Math.Abs(verticalCumulative) < 50) return;

            if (_lockDirection == ManipulationLock.Vertical ||
                _lockDirection == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50)
            {
                _lockDirection = ManipulationLock.Vertical;
                MediaPlayer.Volume += -verticalChange;
                StatusMessage = $"Volume {MediaPlayer.Volume:F0}%";
                return;
            }

            if (MediaPlayer.IsSeekable)
            {
                _lockDirection = ManipulationLock.Horizontal;
                MediaPlayer.ShouldUpdateTime = false;
                var timeChange = horizontalChange * HorizontalChangePerPixel;
                MediaPlayer.Time += timeChange;

                var changeText = HumanizedDurationConverter.Convert(MediaPlayer.Time - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                StatusMessage = $"{HumanizedDurationConverter.Convert(MediaPlayer.Time)} ({changeText})";
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _lockDirection = ManipulationLock.None;
            _timeBeforeManipulation = MediaPlayer?.Time ?? 0;
        }
    }
}
