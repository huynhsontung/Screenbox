// Inspired by https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.platformui.layoutdoubleutil.areclose

using System;
using Windows.Foundation;

namespace Screenbox.UI;

/// <summary>
/// Provides <see langword="static"/> helper methods for working with double values.
/// </summary>
public static class DoubleHelper
{
    private const double FloatMachineEpsilon = 1.1920929E-07;

    /// <summary>
    /// Determines whether two double values are close enough to be considered
    /// equivalent.
    /// </summary>
    /// <param name="value1">The first value to compare.</param>
    /// <param name="value2">The second value to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the values are equal or within tolerance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
        {
            return true;
        }

        return Math.Abs(value1 - value2) <= FloatMachineEpsilon;
    }

    /// <summary>
    /// Determines whether two <see cref="Point"/> values are close enough to be considered
    /// equivalent. The points are considered close if their X and Y coordinates are equal or within tolerance.
    /// </summary>
    /// <param name="point1">The first point to compare.</param>
    /// <param name="point2">The second point to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the points are equal or within tolerance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool AreClose(Point point1, Point point2)
    {
        return AreClose(point1.X, point2.X)
            && AreClose(point1.Y, point2.Y);
    }

    /// <summary>
    /// Determines whether two <see cref="Rect"/> values are close enough to be considered
    /// equivalent. The rectangles are considered close if their origins, and sizes are equal or within tolerance.
    /// </summary>
    /// <param name="rect1">The first rectangle to compare.</param>
    /// <param name="rect2">The second rectangle to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the rectangles are equal or within tolerance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool AreClose(Rect rect1, Rect rect2)
    {
        return AreClose(rect1.X, rect2.X)
            && AreClose(rect1.Y, rect2.Y)
            && AreClose(rect1.Width, rect2.Width)
            && AreClose(rect1.Height, rect2.Height);
    }

    /// <summary>
    /// Determines whether two <see cref="Size"/> values are close enough to be considered
    /// equivalent. The sizes are considered close if their widths and heights are equal or within tolerance.
    /// </summary>
    /// <param name="size1">The first size to compare.</param>
    /// <param name="size2">The second size to compare.</param>
    /// <returns>
    /// <see langword="true"/> when the sizes are equal or within tolerance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool AreClose(Size size1, Size size2)
    {
        return AreClose(size1.Width, size2.Width)
            && AreClose(size1.Height, size2.Height);
    }
}
