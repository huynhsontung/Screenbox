#nullable enable

using System;
using Windows.UI.Xaml.Data;
using Screenbox.Core;

namespace Screenbox.Converters
{
    internal sealed class RendererGlyphConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            Renderer renderer = (Renderer)value;
            return renderer.CanRenderVideo ? "\ue7f4" : "\ue7f5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
