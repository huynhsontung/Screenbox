#nullable enable

using System;
using Windows.UI.Xaml.Data;
using Screenbox.Core;

namespace Screenbox.Converters
{
    internal sealed class HumanizedDurationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is TimeSpan duration)
                return Humanizer.ToDuration(duration);
            return Humanizer.ToDuration(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
