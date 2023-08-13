using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;
internal class PlayPauseGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? "\uE103" : "\uE102";
        }

        return "\uE102";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
