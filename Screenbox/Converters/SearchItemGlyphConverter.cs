using Screenbox.Core.Enums;
using Screenbox.Core.ViewModels;
using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal sealed class SearchItemGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case MediaViewModel media:
                    return media.MediaType == MediaPlaybackType.Music ? "\ue8d6" : "\ue8b2";
                case AlbumViewModel:
                    return "\ue93c";
                case ArtistViewModel:
                    return "\ue77b";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
