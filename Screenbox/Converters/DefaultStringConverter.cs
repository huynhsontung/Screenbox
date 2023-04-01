using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal class DefaultStringConverter : IValueConverter
    {
        public string Default { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null => Default,
                string str => string.IsNullOrEmpty(str) ? Default : str,
                _ => value
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
