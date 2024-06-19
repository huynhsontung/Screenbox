using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;
public class NullVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is not null ^ (parameter as string ?? string.Empty).Equals("Reverse") ?
            Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
