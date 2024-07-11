#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
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
    public LivelyWallpaperModel? Source
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

    public MediaViewModel? Media
    {
        get => (MediaViewModel?)GetValue(MediaProperty);
        set => SetValue(MediaProperty, value);
    }

    public static readonly DependencyProperty MediaProperty =
        DependencyProperty.Register("Media", typeof(MediaViewModel), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(null, OnMediaChanged));

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

    public event EventHandler<Exception>? WallpaperError;

    // We handle the WebView life-cycle by code.
    private WebView2? _webView;
    private bool _isWebViewInitialized;
    private ulong _currentNavigationId;

    private long _propertyChangedToken;

    public LivelyWebWallpaperPlayer()
    {
        this.InitializeComponent();
    }

    private static async void OnMediaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LivelyWebWallpaperPlayer)d;
        if (e.OldValue is MediaViewModel oldMedia) oldMedia.PropertyChanged -= control.MediaOnPropertyChanged;
        if (e.NewValue is MediaViewModel newValue) newValue.PropertyChanged += control.MediaOnPropertyChanged;
        await control.UpdateMusic();
    }

    private async void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MediaViewModel.Name) or nameof(MediaViewModel.MainArtist))
        {
            await UpdateMusic();
        }
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _propertyChangedToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);

        await InitializeWebView2();
        UpdatePage();
    }

    // x:Load xaml can be used to close and or restart WebView process.
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPropertyChangedCallback(VisibilityProperty, _propertyChangedToken);
        if (Media != null) Media.PropertyChanged -= MediaOnPropertyChanged;
        CloseWebView2();
    }

    private async void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        await UpdateVisibility(Visibility);
    }

    private async Task InitializeWebView2()
    {
        try
        {
            if (_webView is not null)
                CloseWebView2();

            _webView = new WebView2();
            _webView.NavigationStarting += WebView_NavigationStarting;
            _webView.NavigationCompleted += WebView_NavigationCompleted;
            LayoutRoot.Children.Add(_webView);

            await _webView.EnsureCoreWebView2Async();

            // Theme need to set css, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4426
            _webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
            // Don't allow contextmenu and devtools.
            _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            // Mute audio output.
            _webView.CoreWebView2.IsMuted = true;
            _webView.CoreWebView2.OpenDevToolsWindow();

            _isWebViewInitialized = true;
        }
        catch (Exception ex)
        {
            WallpaperError?.Invoke(this, ex);
        }
    }

    private void CloseWebView2()
    {
        _webView?.Close();
        LayoutRoot.Children.Remove(_webView);
        _isWebViewInitialized = false;
        _webView = null;
    }

    private void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        _currentNavigationId = args.NavigationId;
        IsLoading = true;
    }

    private async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.NavigationId != _currentNavigationId)
            return;

        await UpdateLivelyProperties();
        await UpdateMusic();
        IsLoading = false;
    }

    private void UpdatePage()
    {
        if (!_isWebViewInitialized || Source is null)
            return;

        var htmlPath = Path.Combine(Source.Path, Source.Model.FileName);
        _webView.NavigateToLocalPath(htmlPath);
    }

    private async Task UpdateVisibility(Visibility visibility)
    {
        if (!_isWebViewInitialized || IsLoading)
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

        // await UpdatePauseState();
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction#controls
    private async Task UpdateLivelyProperties()
    {
        if (Source is null || _webView is null)
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
                    await _webView.ExecuteScriptFunctionAsync(functionName, item.Key, (double)item.Value["value"]);
                    break;
                case "dropdown":
                    await _webView.ExecuteScriptFunctionAsync(functionName, item.Key, (int)item.Value["value"]);
                    break;
                case "checkbox":
                    await _webView.ExecuteScriptFunctionAsync(functionName, item.Key, (bool)item.Value["value"]);
                    break;
                case "color":
                    await _webView.ExecuteScriptFunctionAsync(functionName, item.Key, (string)item.Value["value"]);
                    break;
                case "folderDropdown":
                    var relativePath = Path.Combine(item.Value["folder"].ToString(), item.Value["value"].ToString());
                    var filePath = Path.Combine(Source.Path, relativePath);
                    await _webView.ExecuteScriptFunctionAsync(functionName, item.Key, File.Exists(filePath) ? relativePath : null);
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
        if (Source == null || (_webView?.CoreWebView2.IsSuspended ?? true) || !Source.IsMusic || Visibility == Visibility.Collapsed)
            return;

        LivelyMusicModel? model = null;
        if (Media != null)
        {
            var base64 = await StorageItemToBase64(Media.ThumbnailSource);
            model = new LivelyMusicModel
            {
                Title = Media.Name,
                Artist = Media.MainArtist?.Name,
                Thumbnail = base64,
                // Optional: Complete the mapping.
            };
        }

        await _webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(model));
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--audio
    private async Task UpdateAudio()
    {
        // WebView rendering process pauses when visibility is hidden.
        if (Source == null || (_webView?.CoreWebView2.IsSuspended ?? true) || !Source.IsMusic)
            return;

        await _webView.ExecuteScriptFunctionAsync("livelyAudioListener", Audio);
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--pause-event
    private async Task UpdatePauseState()
    {
        if (Source is null || _webView is null || !Source.IsPauseNotify)
            return;

        // var obj = new LivelyPlaybackStateModel()
        // {
        //     IsPaused = _currentVisibility == Visibility.Collapsed
        // };
        // await _webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged", obj);
    }

    private static async Task<string> StorageItemToBase64(StorageItemThumbnail? item)
    {
        if (item == null)
            return string.Empty;

        using var stream = item.CloneStream();

        var buffer = new byte[stream.Size];
        await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
        return Convert.ToBase64String(buffer);
    }
}
