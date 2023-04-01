#nullable enable

using System;
using Windows.UI.Xaml.Data;
using Screenbox.Strings;

namespace Screenbox.Converters
{
    internal class GenreTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null => Resources.UnknownGenre,
                string str => string.IsNullOrEmpty(str) ? Resources.UnknownGenre : str,
                _ => value
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
