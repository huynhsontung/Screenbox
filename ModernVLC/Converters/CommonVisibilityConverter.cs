using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ModernVLC.Converters
{
    internal class CommonVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                if (Invert)
                {
                    return b ? Visibility.Collapsed : Visibility.Visible;
                }

                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            var result = value is string s ? !string.IsNullOrEmpty(s) : value != null;

            if (Invert)
            {
                return !result ? Visibility.Visible : Visibility.Collapsed;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
