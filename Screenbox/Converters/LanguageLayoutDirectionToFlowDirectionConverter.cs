using System;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

public sealed class LanguageLayoutDirectionToFlowDirectionConverter : IValueConverter
{
    /// <summary>
    /// Converts a <see cref="LanguageLayoutDirection"/> value to its corresponding <see cref="FlowDirection"/>.
    /// </summary>
    /// <param name="value">The <see cref="LanguageLayoutDirection"/> to convert.</param>
    /// <returns><see cref="FlowDirection.RightToLeft"/> if <paramref name="value"/> is <see cref="LanguageLayoutDirection.Rtl"/> or
    /// <see cref="LanguageLayoutDirection.TtbRtl"/>; otherwise, returns <see cref="FlowDirection.LeftToRight"/>.</returns>
    public static FlowDirection ToFlowDirection(LanguageLayoutDirection value)
    {
        return value is LanguageLayoutDirection.Rtl or LanguageLayoutDirection.TtbRtl
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LanguageLayoutDirection direction)
        {
            return ToFlowDirection(direction);
        }

        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
