﻿using Screenbox.Core.Helpers;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal sealed class MediaGlyphConverter : IValueConverter
    {
        public static string Convert(IStorageItem item)
        {
            string glyph = "\ue8b7";
            if (item is IStorageFile file)
            {
                glyph = "\ue8a5";
                if (file.IsSupportedVideo())
                {
                    glyph = "\ue8b2";
                }
                else if (file.IsSupportedAudio())
                {
                    glyph = "\ue8d6";
                }
            }

            return glyph;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IStorageItem item)
            {
                return Convert(item);
            }

            return "\ue774"; // Globe icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
