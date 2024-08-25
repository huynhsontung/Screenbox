using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Screenbox.Core.Helpers;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public static class WebView2Util
{
    public static bool IsWebViewAvailable()
    {
        try
        {
            return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void NavigateToLocalPath(this WebView2 webView, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var fileName = Path.GetFileName(filePath);
        // Use unique hostname to avoid webview cache issues.
        var hostName = new DirectoryInfo(filePath).Parent.Name;
        var directoryPath = Path.GetDirectoryName(filePath);
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            hostName,
            directoryPath,
            CoreWebView2HostResourceAccessKind.Allow);

        webView.CoreWebView2.Navigate($"https://{hostName}/{fileName}");
    }

    // Ref: https://stackoverflow.com/questions/62835549/equivalent-of-webbrowser-invokescriptstring-object-in-webview2
    public static async Task<string> ExecuteScriptFunctionAsync(this WebView2 webView, string functionName, params object[] parameters)
    {
        var script = new StringBuilder();
        script.Append(functionName);
        script.Append("(");
        for (int i = 0; i < parameters.Length; i++)
        {
            script.Append(JsonConvert.SerializeObject(parameters[i]));
            if (i < parameters.Length - 1)
            {
                script.Append(", ");
            }
        }
        script.Append(");");
        return await webView.ExecuteScriptAsync(script.ToString());
    }

    public static async Task<bool> DownloadWebView()
    {
        // Do not install WebView2 directly, possible Microsoft Store policy violation (?.)
        var uri = new Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
        return await Launcher.LaunchUriAsync(uri);
    }
}
