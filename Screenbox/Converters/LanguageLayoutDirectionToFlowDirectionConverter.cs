using System;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;
internal class LanguageLayoutDirectionToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is (LanguageLayoutDirection.Rtl or LanguageLayoutDirection.TtbRtl))
        {
            return FlowDirection.RightToLeft;
        }

        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
