using System;
using Windows.Media;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal class MediaTypeAdaptiveLayoutOverrideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MediaPlaybackType mediaType && mediaType != MediaPlaybackType.Music)
                return 0;
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
