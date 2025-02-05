// Converter ported to C#.
// Source: https://github.com/microsoft/microsoft-ui-xaml/pull/6829

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Screenbox.Converters;

/// <summary>
/// Value converter that takes a <see cref="Thickness"/> value and returns a modified value.
/// Can be used to convert a <see cref="Thickness"/>, side, or edge value to zero.
/// </summary>
public class ThicknessFilterConverter : IValueConverter
{
    public enum ThicknessFilterKind { None, Left, Top, Right, Bottom, LeftRight, TopBottom };

    /// <summary>
    /// Identifies the <see cref="FilterProperty"/> property.
    /// </summary>
    public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
        nameof(Filter), typeof(ThicknessFilterKind), typeof(ThicknessFilterConverter), new PropertyMetadata(null));

    public ThicknessFilterKind Filter { get; set; } = ThicknessFilterKind.None;

    public Thickness ExcludeConvert(Thickness thickness, ThicknessFilterKind filterKind)
    {
        Thickness result = thickness;

        switch (filterKind)
        {
            case ThicknessFilterKind.None:
                break;
            case ThicknessFilterKind.Left:
                result.Left = 0;
                break;
            case ThicknessFilterKind.Top:
                result.Top = 0;
                break;
            case ThicknessFilterKind.Right:
                result.Right = 0;
                break;
            case ThicknessFilterKind.Bottom:
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.LeftRight:
                result.Left = 0;
                result.Right = 0;
                break;
            case ThicknessFilterKind.TopBottom:
                result.Top = 0;
                result.Bottom = 0;
                break;
        }

        return result;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Thickness thickness)
        {
            return ExcludeConvert(thickness, Filter);
        }
        else
        {
            return value;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
