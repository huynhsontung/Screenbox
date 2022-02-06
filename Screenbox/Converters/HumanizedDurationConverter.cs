using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal class HumanizedDurationConverter : IValueConverter
    {
        public static string Convert(double value)
        {
            var negative = value < 0;
            value = Math.Abs(value);
            var hours = (long)(value / 3.6e+6);
            var duration = TimeSpan.FromMilliseconds(value);
            return (negative ? "-" : string.Empty) + (hours > 0 ? $"{hours}:{duration:%m}:{duration:ss}" : duration.ToString(@"%m\:ss"));
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Convert(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
