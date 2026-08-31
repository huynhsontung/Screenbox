using System;
using CommunityToolkit.Diagnostics;
using Windows.Media;
using Windows.UI.Xaml.Data;
using WinRT;

namespace Screenbox.Converters;

internal sealed partial class ToggleButtonCheckToRepeatModeConverter : IValueConverter
{
    [DynamicWindowsRuntimeCast(typeof(MediaPlaybackAutoRepeatMode))]
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
