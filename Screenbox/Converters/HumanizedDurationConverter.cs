#nullable enable

using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal sealed class HumanizedDurationConverter : IValueConverter
    {
        public static string Convert(double value)
        {
            TimeSpan duration = TimeSpan.FromMilliseconds(value);
            return Convert(duration);
        }

        public static string Convert(TimeSpan duration)
        {
            int hours = (int)duration.TotalHours;
            return (duration < TimeSpan.Zero ? "-" : string.Empty) + (hours > 0 ? $"{hours}:{duration:mm}:{duration:ss}" : duration.ToString(@"%m\:ss"));
        }

        public object? Convert(object? value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            if (value is TimeSpan duration)
                return Convert(duration);
            return Convert(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
