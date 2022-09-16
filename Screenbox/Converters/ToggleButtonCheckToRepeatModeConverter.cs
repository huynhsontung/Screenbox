#nullable enable

using System;
using Windows.Media;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Converters
{
    internal class ToggleButtonCheckToRepeatModeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            Guard.IsOfType<MediaPlaybackAutoRepeatMode>(value, nameof(value));
            MediaPlaybackAutoRepeatMode repeatMode = (MediaPlaybackAutoRepeatMode)value;
            switch (repeatMode)
            {
                case MediaPlaybackAutoRepeatMode.None:
                    return false;
                case MediaPlaybackAutoRepeatMode.List:
                    return true;
                case MediaPlaybackAutoRepeatMode.Track:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object? value, Type targetType, object parameter, string language)
        {
            if (value == null) return MediaPlaybackAutoRepeatMode.Track;
            bool check = (bool)value;
            return check ? MediaPlaybackAutoRepeatMode.List : MediaPlaybackAutoRepeatMode.None;
        }
    }
}
