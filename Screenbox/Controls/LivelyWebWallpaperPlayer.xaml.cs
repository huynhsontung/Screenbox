#nullable enable

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Screenbox.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWebWallpaperPlayer : UserControl
{
    // public double[] Audio
    // {
    //     get { return (double[])GetValue(AudioProperty); }
    //     set
    //     {
    //         SetValue(AudioProperty, value);
    //         _ = UpdateAudio();
    //     }
    // }
    //
    // public static readonly DependencyProperty AudioProperty =
    //     DependencyProperty.Register("Audio", typeof(double[]), typeof(LivelyWebWallpaperPlayer), new PropertyMetadata(Array.Empty<double>()));

    internal LivelyWallpaperPlayerViewModel ViewModel => (LivelyWallpaperPlayerViewModel)DataContext;

    // We handle the WebView life-cycle by code.
    private WebView2? _webView;
    private bool _isWebViewInitialized;
    private ulong _currentNavigationId;

    private long _propertyChangedToken;

    public LivelyWebWallpaperPlayer()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<LivelyWallpaperPlayerViewModel>();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _propertyChangedToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);

        await InitializeWebView2();
        await ViewModel.LoadAsync();
        await UpdatePage();

        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.TrackUpdateRequested += ViewModelOnTrackUpdateRequested;
    }

    // x:Load xaml can be used to close and or restart WebView process.
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPropertyChangedCallback(VisibilityProperty, _propertyChangedToken);
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.TrackUpdateRequested -= ViewModelOnTrackUpdateRequested;
        CloseWebView2();
    }

    private async void ViewModelOnTrackUpdateRequested(object sender, EventArgs e)
    {
        await UpdateCurrentTrack();
    }

    private async void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.Source):
                await UpdatePage();
                break;
        }
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
            _webView.FlowDirection = FlowDirection.LeftToRight;
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
            // _webView.CoreWebView2.OpenDevToolsWindow();

            _isWebViewInitialized = true;
        }
        catch (Exception ex)
        {
            ViewModel.SendError(Strings.Resources.FailedToLoadVisualNotificationTitle, ex.Message);
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
        ViewModel.IsLoading = true;
    }

    private async void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.NavigationId != _currentNavigationId)
            return;

        await ViewModel.UpdateLivelyProperties(sender);
        await UpdateCurrentTrack();
        ViewModel.IsLoading = false;
    }

    private async Task UpdatePage()
    {
        if (!_isWebViewInitialized || _webView == null)
            return;

        try
        {
            await ViewModel.NavigatePage(_webView);
        }
        catch (Exception e)
        {
            ViewModel.SendError(Strings.Resources.FailedToLoadVisualNotificationTitle, e.Message);
        }
    }

    private async Task UpdateVisibility(Visibility visibility)
    {
        if (!_isWebViewInitialized || ViewModel.IsLoading)
            return;

        switch (visibility)
        {
            case Visibility.Visible:
                // WebView rendering stops while hidden/minimized but JS can still execute.
                // To avoid queuing music change code we don't sent update while the control is hidden and only update one time once visible.
                // This does mean there will be a brief duration in which previous albumart will be visible, workaround - change Opacity instead.
                await UpdateCurrentTrack();
                break;
            case Visibility.Collapsed:
                // This is not required, ref: https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.trysuspendasync
                // await WebView.CoreWebView2.TrySuspendAsync();
                break;
        }

        await UpdatePauseState();
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--system-nowplaying
    private async Task UpdateCurrentTrack()
    {
        // WebView rendering process pauses when visibility is hidden.
        if (_webView == null || Visibility == Visibility.Collapsed)
            return;

        await ViewModel.UpdateCurrentTrack(_webView);
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--audio
    // private async Task UpdateAudio()
    // {
    //     // WebView rendering process pauses when visibility is hidden.
    //     if (ViewModel.Source == null || (_webView?.CoreWebView2.IsSuspended ?? true) || !ViewModel.Source.IsMusic)
    //         return;
    //
    //     await _webView.ExecuteScriptFunctionAsync("livelyAudioListener", Audio);
    // }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--pause-event
    private async Task UpdatePauseState()
    {
        if (_webView is null) return;
        await ViewModel.UpdatePauseState(_webView, Visibility == Visibility.Collapsed);
    }
}
