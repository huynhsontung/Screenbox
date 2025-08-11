// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp.UI/Extensions/ApplicationViewExtensions.cs

using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Extensions;

/// <summary>
/// Provides attached properties for interacting with the <see cref="Windows.UI.ViewManagement.ApplicationView"/> on a window (app view).
/// </summary>
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
