using System;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using CommunityToolkit.Diagnostics;

namespace Screenbox.Converters
{
    internal sealed class StorageItemGlyphConverter : IValueConverter
    {
        public static string Convert(IStorageItem item)
        {
            string glyph = "\ue8b7";
            if (item is IStorageFile file)
            {
                glyph = "\ue8a5";
                if (file.ContentType.StartsWith("video"))
                {
                    glyph = "\ue8b2";
                }
                else if (file.ContentType.StartsWith("audio"))
                {
                    glyph = "\ue8d6";
                }
            }

            return glyph;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Guard.IsAssignableToType<IStorageItem>(value);
            return Convert((IStorageItem)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
