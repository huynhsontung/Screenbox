using Screenbox.Core.Enums;
using Screenbox.Core.Helpers;
using Screenbox.Core.ViewModels;
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

        public static string Convert(MediaPlaybackType type)
        {
            return type switch
            {
                MediaPlaybackType.Music => "\ue8d6",
                MediaPlaybackType.Video => "\ue8b2",
                MediaPlaybackType.Image => "\ue91b",
                _ => "\ue8a5"
            };
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                IStorageItem item => Convert(item),
                FileMediaViewModel { File: var file } => Convert(file),
                // UriMediaViewModel { Uri.IsFile: true, MediaType: var mediaType } => Convert(mediaType),
                MediaViewModel { IsFromLibrary: true, MediaType: var mediaType } => Convert(mediaType),
                _ => "\ue774"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
