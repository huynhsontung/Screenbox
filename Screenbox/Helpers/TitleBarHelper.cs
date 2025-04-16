using Windows.UI;
using Windows.UI.Xaml;

namespace Screenbox.Helpers;

/// <summary>
/// A helper that provides methods to customize the title bar.
/// </summary>
public static class TitleBarHelper
{
    /// <summary>
    /// Hide the default title bar and extend the client area to fill the entire view.
    /// </summary>
    //public static void ExtendViewIntoTitleBar()
    //{
    //    var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
    //    if (coreTitleBar != null)
    //    {
    //        coreTitleBar.ExtendViewIntoTitleBar = true;
    //    }
    //}

    /// <summary>
    /// Set subtle colors to the title bar buttons (system caption buttons).
    /// Applicable only when the view is extended into the title bar.
    /// </summary>
    /// <param name="element">The base element from which to retrieve the <see cref="FrameworkElement.RequestedTheme"/> property.</param>
    public static void SetCaptionButtonColors(FrameworkElement element)
    {
        var buttonBackgroundColor = Colors.Transparent;

        var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
        if (titleBar != null)
        {
            titleBar.ButtonBackgroundColor = buttonBackgroundColor;
            titleBar.ButtonInactiveBackgroundColor = buttonBackgroundColor;

            // Only the background colors respect the alpha channel, the foreground colors are flattened
            // by layering them over the SolidBackgroundFillColorBase and its corresponding background color.
            if (element.ActualTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // TextFillColorPrimary

                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF); // SubtleFillColorSecondary
                titleBar.ButtonHoverForegroundColor = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF); // TextFillColorPrimary

                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF); // SubtleFillColorTertiary
                titleBar.ButtonPressedForegroundColor = Color.FromArgb(0xFF, 0xD1, 0xD1, 0xD1); // TextFillColorSecondary

                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x71, 0x71, 0x71); // TextFillColorTertiary
            }
            else
            {
                titleBar.ButtonForegroundColor = Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A); // TextFillColorPrimary

                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x09, 0x00, 0x00, 0x00); // SubtleFillColorSecondary
                titleBar.ButtonHoverForegroundColor = Color.FromArgb(0xFF, 0x19, 0x19, 0x19); // TextFillColorPrimary

                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x06, 0x00, 0x00, 0x00); // SubtleFillColorTertiary
                titleBar.ButtonPressedForegroundColor = Color.FromArgb(0xFF, 0x5D, 0x5D, 0x5D); // TextFillColorSecondary

                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x9B, 0x9B, 0x9B); // TextFillColorTertiary
            }
        }
    }

    /// <summary>
    /// Set the the system default colors to the title bar buttons.
    /// </summary>
    //public static void ResetCaptionButtonColors()
    //{
    //    var titleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
    //    if (titleBar != null)
    //    {
    //        titleBar.ButtonBackgroundColor = null;
    //        titleBar.ButtonForegroundColor = null;

    //        titleBar.ButtonHoverBackgroundColor = null;
    //        titleBar.ButtonHoverForegroundColor = null;

    //        titleBar.ButtonPressedBackgroundColor = null;
    //        titleBar.ButtonPressedForegroundColor = null;

    //        titleBar.ButtonInactiveBackgroundColor = null;
    //        titleBar.ButtonInactiveForegroundColor = null;
    //    }
    //}
}
