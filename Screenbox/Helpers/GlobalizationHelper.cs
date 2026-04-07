#nullable enable

using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods related to globalization.
/// </summary>
public static class GlobalizationHelper
{
    /// <summary>
    /// Gets a value that indicates whether the text direction for the current language
    /// is right-to-left.
    /// </summary>
    /// <value><see langword="true"/> if the text direction is right-to-left; otherwise, <see langword="false"/>.</value>
    public static readonly bool IsRightToLeftLanguage = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// Gets the <see cref="FlowDirection"/> for the current application language.
    /// </summary>
    /// <returns>A value that indicates the content flow direction.</returns>
    public static FlowDirection GetFlowDirection()
    {
        return IsRightToLeftLanguage ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    /// <summary>
    /// Mirrors the horizontal placement of the specified <see cref="FlyoutPlacementMode"/>
    /// for right-to-left (RTL) layouts.
    /// </summary>
    /// <param name="placement">A value of the enumeration that specifies the placement of the flyout.</param>
    /// <returns>
    /// A mirrored horizontal placement value for the <see cref="FlyoutBase.Placement"/> property
    /// if <see cref="IsRightToLeftLanguage"/> is <see langword="true"/>; otherwise, returns the
    /// original value.
    /// </returns>
    public static FlyoutPlacementMode MirrorFlyoutPlacementWhenRightToLeft(FlyoutPlacementMode placement)
    {
        if (!IsRightToLeftLanguage)
        {
            return placement;
        }

        return placement switch
        {
            FlyoutPlacementMode.Left => FlyoutPlacementMode.Right,
            FlyoutPlacementMode.Right => FlyoutPlacementMode.Left,
            FlyoutPlacementMode.TopEdgeAlignedLeft => FlyoutPlacementMode.TopEdgeAlignedRight,
            FlyoutPlacementMode.TopEdgeAlignedRight => FlyoutPlacementMode.TopEdgeAlignedLeft,
            FlyoutPlacementMode.BottomEdgeAlignedLeft => FlyoutPlacementMode.BottomEdgeAlignedRight,
            FlyoutPlacementMode.BottomEdgeAlignedRight => FlyoutPlacementMode.BottomEdgeAlignedLeft,
            FlyoutPlacementMode.LeftEdgeAlignedTop => FlyoutPlacementMode.RightEdgeAlignedTop,
            FlyoutPlacementMode.LeftEdgeAlignedBottom => FlyoutPlacementMode.RightEdgeAlignedBottom,
            FlyoutPlacementMode.RightEdgeAlignedTop => FlyoutPlacementMode.LeftEdgeAlignedTop,
            FlyoutPlacementMode.RightEdgeAlignedBottom => FlyoutPlacementMode.LeftEdgeAlignedBottom,
            _ => placement,
        };
    }
}
