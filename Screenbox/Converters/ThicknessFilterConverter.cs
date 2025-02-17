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
    public enum ThicknessFilterKind
    {
        None,
        Left,
        Top,
        Right,
        Bottom,
        LeftRight,
        TopBottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        LeftRightTop,
        LeftRightBottom,
        TopBottomLeft,
        TopBottomRight,
    }

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
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Top:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Right:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.Bottom:
                result.Left = 0;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.LeftRight:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.TopBottom:
                result.Left = 0;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopLeft:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.TopRight:
                result.Left = 0;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.BottomLeft:
                //result.Left = thickness.Left;
                result.Top = 0;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.BottomRight:
                result.Left = 0;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.LeftRightTop:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                result.Bottom = 0;
                break;
            case ThicknessFilterKind.LeftRightBottom:
                //result.Left = thickness.Left;
                result.Top = 0;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopBottomLeft:
                //result.Left = thickness.Left;
                //result.Top = thickness.Top;
                result.Right = 0;
                //result.Bottom = thickness.Bottom;
                break;
            case ThicknessFilterKind.TopBottomRight:
                result.Left = 0;
                //result.Top = thickness.Top;
                //result.Right = thickness.Right;
                //result.Bottom = thickness.Bottom;
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
