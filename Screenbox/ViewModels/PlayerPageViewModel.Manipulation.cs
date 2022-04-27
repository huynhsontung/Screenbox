using System;
using Windows.UI.Xaml.Input;
using Screenbox.Converters;
using Screenbox.Core.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Screenbox.ViewModels
{
    internal partial class PlayerPageViewModel
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
        private bool _overrideStatusTimeout;

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _overrideStatusTimeout = false;
            if (_lockDirection == ManipulationLock.None) return;
            OverrideVisibilityChange(100);
            ShowStatusMessage(null);
            Messenger.Send(new ChangeSeekBarInteractionRequestMessage { Value = false });
        }

        public void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var horizontalChange = e.Delta.Translation.X;
            var verticalChange = e.Delta.Translation.Y;
            var horizontalCumulative = e.Cumulative.Translation.X;
            var verticalCumulative = e.Cumulative.Translation.Y;
            if (Math.Abs(horizontalCumulative) < 50 && Math.Abs(verticalCumulative) < 50) return;

            if (_lockDirection == ManipulationLock.Vertical ||
                _lockDirection == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50)
            {
                _lockDirection = ManipulationLock.Vertical;
                ChangeVolume(-verticalChange);
                return;
            }

            if (VlcPlayer?.IsSeekable ?? false)
            {
                _lockDirection = ManipulationLock.Horizontal;
                Messenger.Send(new ChangeSeekBarInteractionRequestMessage { Value = true });
                var timeChange = horizontalChange * HorizontalChangePerPixel;
                long newTime = _mediaPlayerService.Seek(timeChange);
                Messenger.Send(new ChangeTimeRequestMessage { Value = newTime });

                var changeText = HumanizedDurationConverter.Convert(newTime - _timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                ShowStatusMessage($"{HumanizedDurationConverter.Convert(newTime)} ({changeText})");
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _overrideStatusTimeout = true;
            _lockDirection = ManipulationLock.None;
            _timeBeforeManipulation = VlcPlayer?.Time ?? 0;
        }
    }
}
