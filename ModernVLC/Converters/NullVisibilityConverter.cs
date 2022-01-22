using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ModernVLC.Converters
{
    internal class NullVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
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
