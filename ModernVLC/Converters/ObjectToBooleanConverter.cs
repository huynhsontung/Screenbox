using System;
using Windows.UI.Xaml.Data;

namespace ModernVLC.Converters
{
    internal class ObjectToBooleanConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return Invert ? !b : b;
            }

            var result = value is string s ? !string.IsNullOrEmpty(s) : value != null;

            return Invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
