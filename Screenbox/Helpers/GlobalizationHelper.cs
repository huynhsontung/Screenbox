#nullable enable

using System.Globalization;
using Windows.UI.Xaml;

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
}
