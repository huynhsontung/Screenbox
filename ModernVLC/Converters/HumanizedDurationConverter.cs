using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace ModernVLC.Converters
{
    internal class HumanizedDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var numeric = System.Convert.ToDouble(value);
            var hours = (long)(numeric / 3.6e+6);
            var duration = TimeSpan.FromMilliseconds(numeric);
            return hours > 0 ? $"{hours}:{duration:%m}:{duration:ss}" : duration.ToString(@"%m\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
