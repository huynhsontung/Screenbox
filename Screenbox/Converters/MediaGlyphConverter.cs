using System;
using Screenbox.Core.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    internal sealed class MediaGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null => "\uea69",
                IStorageItem item => GlyphConverter.ToStorageItemGlyph(item),
                MediaViewModel viewModel => GlyphConverter.ToMediaGlyph(viewModel),
                _ => "\ue774"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
