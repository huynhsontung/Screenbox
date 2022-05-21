using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
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

            bool result = value is string s ? !string.IsNullOrEmpty(s) : System.Convert.ToBoolean(value);

            return Invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
