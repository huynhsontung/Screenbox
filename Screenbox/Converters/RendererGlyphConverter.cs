#nullable enable

using Screenbox.Casting.Abstractions;
using Screenbox.Casting.Models;
using System;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters
{
    /// <summary>
    /// Returns a Segoe MDL2 Assets glyph for an <see cref="ICastDevice"/> instance.
    /// Chromecast devices get a TV/monitor glyph; DLNA devices get a speaker glyph.
    /// </summary>
    internal sealed class RendererGlyphConverter : IValueConverter
    {
        // Segoe MDL2 Assets: TV / monitor — used for Chromecast renderers.
        private const string ChromecastGlyph = "\uE7F4";

        // Segoe MDL2 Assets: Speaker / audio — used for DLNA/UPnP renderers.
        private const string DlnaGlyph = "\uE767";

        public object? Convert(object? value, Type targetType, object parameter, string language)
        {
            if (value is ICastDevice device)
            {
                return device.Type == CastDeviceType.Dlna ? DlnaGlyph : ChromecastGlyph;
            }

            return ChromecastGlyph;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
