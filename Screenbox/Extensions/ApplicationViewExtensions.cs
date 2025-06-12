using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Extensions;
public static class ApplicationViewExtensions
{
    /// <summary>
    /// Gets <see cref="string"/> for <see cref="Windows.UI.ViewManagement.ApplicationView.Title"/>
    /// </summary>
    /// <param name="page">The <see cref="Page"/></param>
    /// <returns><see cref="string"/></returns>
    public static string GetTitle(Page page)
    {
        ApplicationView? applicationView = GetApplicationView();

        return applicationView?.Title ?? string.Empty;
    }

    /// <summary>
    /// Sets <see cref="string"/> to <see cref="Windows.UI.ViewManagement.ApplicationView.Title"/>
    /// </summary>
    /// <param name="page">The <see cref="Page"/></param>
    /// <param name="value"><see cref="string"/></param>
    public static void SetTitle(Page page, string value)
    {
        ApplicationView? applicationView = GetApplicationView();
        if (applicationView != null)
        {
            applicationView.Title = value;
        }
    }

    private static ApplicationView? GetApplicationView()
    {
        return ApplicationView.GetForCurrentView();
    }
}
