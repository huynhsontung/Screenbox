using System.Globalization;
using Windows.UI.Xaml;

namespace Screenbox.Helpers;

/// <summary>
/// Provides <see langword="static"/> helper methods related to globalization.
/// </summary>
public static class GlobalizationHelper
{
    /// <summary>
    /// Gets whether the text direction for the current app's language is right-to-left (RTL).
    /// </summary>
    /// <returns><see langword="true"/>, if the text direction is right-to-left; otherwise, <see langword="false"/>.</returns>
    public static readonly bool IsRightToLeftLanguage = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;

    /// <summary>
    /// Gets the <see cref="FlowDirection"/> based on the text directionality of the app's display language.
    /// </summary>
    /// <returns>
    /// <see cref="FlowDirection.RightToLeft"/> if <see cref="IsRightToLeftLanguage"/> is <see langword="true"/>;
    /// otherwise, <see cref="FlowDirection.LeftToRight"/>.
    /// </returns>
    public static FlowDirection GetFlowDirection()
    {
        return IsRightToLeftLanguage ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
}
