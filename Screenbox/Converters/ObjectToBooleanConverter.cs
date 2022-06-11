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

            if (value is string s)
            {
                bool sResult = string.IsNullOrEmpty(s);
                return Invert ? sResult : !sResult;
            }

            bool result;
            if (value != null && value.GetType().IsPrimitive)
            {
                result = System.Convert.ToBoolean(value);
            }
            else
            {
                result = value != null;
            }

            return Invert ? !result : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
