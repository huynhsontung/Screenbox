#nullable enable

using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal class CommonVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case ICollection collection:
                    return GetVisibility(collection.Count > 0);
                case bool b:
                    return GetVisibility(b);
                default:
                {
                    var result = value is string s ? !string.IsNullOrEmpty(s) : value != null;

                    return GetVisibility(result);
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private Visibility GetVisibility(bool result)
        {
            if (Invert)
            {
                return !result ? Visibility.Visible : Visibility.Collapsed;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
