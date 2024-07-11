using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWebWallpaperPlayer : UserControl
{
    public LivelyWallpaperModel Source
    {
        get { return (LivelyWallpaperModel)GetValue(SourceProperty); }
        set
        {
            SetValue(SourceProperty, value);
            UpdatePage();
        }
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(LivelyWallpaperModel), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(null));

    public MediaViewModel Media
    {
        get { return (MediaViewModel)GetValue(MediaProperty); }
        set
        {
            SetValue(MediaProperty, value);
            _ = UpdateMusic();
        }
    }

    public static readonly DependencyProperty MediaProperty =
        DependencyProperty.Register("Media", typeof(MediaViewModel), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(null));

    public double[] Audio
    {
        get { return (double[])GetValue(AudioProperty); }
        set 
        { 
            SetValue(AudioProperty, value);
            _ = UpdateAudio();
        }
    }

    public static readonly DependencyProperty AudioProperty =
        DependencyProperty.Register("Audio", typeof(double[]), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(Array.Empty<double>()));

    public bool IsLoading
    {
        get { return (bool)GetValue(IsLoadingProperty); }
        private set { SetValue(IsLoadingProperty, value); }
    }

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register("IsLoading", typeof(bool), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(false));

    // Use this in case control VisibilityProperty is not helpful, ie parent control visibility changed.
    // WebView rendering process stops if parent is Collapsed even without this, but we need this to queue wallpaper events.
    // This property may not be required in the future, ref: https://github.com/microsoft/microsoft-ui-xaml/issues/674
    public Visibility EffectiveVisibility
    {
        get { return (Visibility)GetValue(EffectiveVisibilityProperty); }
        set 
        { 
            SetValue(EffectiveVisibilityProperty, value);
            _ = UpdateVisibility(value);
        }
    }

    public static readonly DependencyProperty EffectiveVisibilityProperty =
        DependencyProperty.Register("EffectiveVisibility", typeof(Visibility), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(Visibility.Visible));

    public event EventHandler<Exception> WallpaperError;

    // We handle the WebView life-cycle by code.
    private WebView2 webView;
    private bool isWebViewInitialized;
    private ulong currentNavigationId;
    // See EffectiveVisibility
    private Visibility currentVisibility;

    public LivelyWebWallpaperPlayer()
    {
        this.InitializeComponent();
        this.RegisterPropertyChangedCallback(VisibilityProperty, UserControl_VisibilityChanged);
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebView2();
        UpdatePage();
    }

    // x:Load xaml can be used to close and or restart WebView process.
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CloseWebView2();
    }

    private async void UserControl_VisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        await UpdateVisibility(((UIElement)sender).Visibility);
    }

    private async Task InitializeWebView2()
    {
        try
        {
            if (webView is not null)
                CloseWebView2();

            webView = new WebView2();
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            LayoutRoot.Children.Add(webView);

            await webView.EnsureCoreWebView2Async();

            // Theme need to set css, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4426
            webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            // Don't allow contextmenu and devtools.
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            // Mute audio output.
            webView.CoreWebView2.IsMuted = true;
            // webView.CoreWebView2.OpenDevToolsWindow();

            isWebViewInitialized = true;
        }
        catch (Exception ex)
        {
            WallpaperError?.Invoke(this, ex);
        }
    }

    private void CloseWebView2()
    {
        webView.Close();
        LayoutRoot.Children.Remove(webView);
        isWebViewInitialized = false;
        webView = null;
    }

    private void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        currentNavigationId = args.NavigationId;
        IsLoading = true;
    }

    private async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.NavigationId != currentNavigationId)
            return;

        await UpdateLivelyProperties();
        await UpdateMusic();
        IsLoading = false;
    }

    private void UpdatePage()
    {
        if (!isWebViewInitialized || Source is null)
            return;

        var htmlPath = Path.Combine(Source.Path, Source.Model.FileName);
        webView.NavigateToLocalPath(htmlPath);
    }

    private async Task UpdateVisibility(Visibility visibility)
    {
        currentVisibility = visibility;
        if (!isWebViewInitialized)
            return;

        switch (visibility)
        {
            case Visibility.Visible:
                {
                    // WebView rendering stops while hidden/minimized but JS can still execute.
                    // To avoid queuing music change code we don't sent update while the control is hidden and only update one time once visible.
                    // This does mean there will be a brief duration in which previous albumart will be visible, workaround - change Opacity instead.
                    await UpdateMusic();
                }
                break;
            case Visibility.Collapsed:
                {
                    // This is not required, ref: https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.trysuspendasync
                    // await WebView.CoreWebView2.TrySuspendAsync();
                }
                break;
        }
        await UpdatePauseState();
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction#controls
    private async Task UpdateLivelyProperties()
    {
        if (Source is null || webView is null)
            return;

        var functionName = "livelyPropertyListener";
        var propertyPath = Path.Combine(Source.Path, "LivelyProperties.json");
        if (!File.Exists(propertyPath))
            return;

        var jsonString = File.ReadAllText(propertyPath);
        var jsonObject = JObject.Parse(jsonString);
        foreach (var item in jsonObject)
        {
            switch (item.Value["type"].ToString())
            {
                case "slider":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (double)item.Value["value"]);
                    break;
                case "dropdown":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (int)item.Value["value"]);
                    break;
                case "checkbox":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (bool)item.Value["value"]);
                    break;
                case "color":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (string)item.Value["value"]);
                    break;
                case "folderDropdown":
                    var relativePath = Path.Combine(item.Value["folder"].ToString(), item.Value["value"].ToString());
                    var filePath = Path.Combine(Source.Path, relativePath);
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, File.Exists(filePath) ? relativePath : null);
                    break;
                case "button":
                case "label":
                    // Ignore, user action only.
                    break;
            }
        }
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--system-nowplaying
    private async Task UpdateMusic()
    {
        // WebView rendering process pauses when visibility is hidden.
        if (Source is null || webView is null || !Source.IsMusic || currentVisibility == Visibility.Collapsed)
            return;

        var base64 = await StorageItemToBase64(this.Media?.ThumbnailSource);
        var obj = Media is null ? null : new LivelyMusicModel()
        {
            Title = Media.Name,
            Artist = Media.Album?.ArtistName,
            Thumbnail = base64,
            // Optional: Complete the mapping.
        };
        await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--audio
    private async Task UpdateAudio()
    {
        // WebView rendering process pauses when visibility is hidden.
        if (Source is null || webView is null || !Source.IsAudio || currentVisibility == Visibility.Collapsed)
            return;

        await webView.ExecuteScriptFunctionAsync("livelyAudioListener", Audio);
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--pause-event
    private async Task UpdatePauseState()
    {
        if (Source is null || webView is null || !Source.IsPauseNotify)
            return;

        var obj = new LivelyPlaybackStateModel()
        {
            IsPaused = currentVisibility == Visibility.Collapsed
        };
        await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged", obj);
    }

    private async Task<string> StorageItemToBase64(StorageItemThumbnail item)
    {
        if (item == null)
            return null;

        using var memoryStream = new MemoryStream();
        using var inputStream = item.CloneStream();

        var buffer = new byte[inputStream.Size];
        await inputStream.ReadAsync(buffer.AsBuffer(), (uint)inputStream.Size, InputStreamOptions.None);

        await memoryStream.WriteAsync(buffer, 0, buffer.Length);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
