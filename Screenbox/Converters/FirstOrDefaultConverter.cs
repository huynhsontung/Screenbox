using System;
using System.Collections;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;
internal class FirstOrDefaultConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        switch (value)
        {
            case IList list:
                return list.Count > 0 ? list[0] : null;
            case IEnumerable enumerable:
                {
                    var enumerator = enumerable.GetEnumerator();
                    using var disposable = enumerator as IDisposable;
                    var current = enumerator.Current;
                    return current;
                }
            default:
                return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
