#nullable enable

using Screenbox.Core.Models;
using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    /// <summary>
    /// Returns a Segoe MDL2 Assets glyph for a <see cref="Renderer"/> instance.
    /// Since SharpCaster targets Chromecast devices (which always support both video and audio),
    /// the TV / monitor glyph (\uE7F4) is always returned.
    /// </summary>
    internal sealed class RendererGlyphConverter : IValueConverter
    {
        // Segoe MDL2 Assets: TV / monitor icon — used for all Chromecast renderers.
        private const string ChromecastGlyph = "\ue7f4";

        public object? Convert(object? value, Type targetType, object parameter, string language)
        {
            if (value is null) return null;
            return ChromecastGlyph;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

