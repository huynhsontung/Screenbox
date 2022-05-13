using System;
using Windows.UI.Xaml.Data;
using Screenbox.ViewModels;

namespace Screenbox.Converters
{
    internal class MediaTypeGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MediaType mediaType = (MediaType)value;
            switch (mediaType)
            {
                case MediaType.Audio:
                    return "\ue8d6";
                case MediaType.Video:
                    return "\ue8b2";
                case MediaType.Network:
                    return "\ue774";
                default:
                    return "\ue9ce";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
