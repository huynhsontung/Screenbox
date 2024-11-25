#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Screenbox.Core.ViewModels;
public partial class LivelyWallpaperPlayerViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>
{
    [ObservableProperty] private LivelyWallpaperModel? _source;
    [ObservableProperty] private bool _isLoading;

    private readonly ILivelyWallpaperService _livelyService;
    private readonly ISettingsService _settingsService;

    public LivelyWallpaperPlayerViewModel(ILivelyWallpaperService livelyService, ISettingsService settingsService)
    {
        _livelyService = livelyService;
        _settingsService = settingsService;

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        Source = message.NewValue;
    }

    public void SendError(string title, string message)
    {
        Messenger.Send(new ErrorMessage(title, message));
    }

    public async Task LoadAsync()
    {
        var allVisualizers = await _livelyService.GetAvailableVisualizersAsync();
        var activeVisualizerPath = _settingsService.LivelyActivePath;

        var selectedVisualizer =
            allVisualizers.FirstOrDefault(visualizer =>
                visualizer.Path.Equals(activeVisualizerPath, StringComparison.OrdinalIgnoreCase));

        Source = selectedVisualizer;
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction#controls
    public async Task UpdateLivelyProperties(WebView2 webView)
    {
        if (Source is null)
            return;

        var functionName = "livelyPropertyListener";
        var propertyPath = Path.Combine(Source.Path, "LivelyProperties.json");

        string jsonString;
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(propertyPath);
            jsonString = await FileIO.ReadTextAsync(file);
        }
        catch (Exception)
        {
            return;
        }

        var jsonObject = JObject.Parse(jsonString);
        foreach (KeyValuePair<string, JToken?> item in jsonObject)
        {
            var typeToken = item.Value?["type"];
            var valueToken = item.Value?["value"];
            if (typeToken == null || valueToken == null) continue;
            switch (typeToken.ToString())
            {
                case "slider":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (double)valueToken);
                    break;
                case "dropdown":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (int)valueToken);
                    break;
                case "checkbox":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (bool)valueToken);
                    break;
                case "color":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, valueToken.ToString());
                    break;
                case "folderDropdown":
                    var relativePath = Path.Combine(item.Value?["folder"]?.ToString() ?? string.Empty, valueToken.ToString());
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

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--pause-event
    public async Task UpdatePauseState(WebView2 webView, bool isPaused)
    {
        if (Source is null || !Source.IsPauseNotify)
            return;

        var obj = new LivelyPlaybackStateModel()
        {
            IsPaused = isPaused
        };
        await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged", obj);
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--system-nowplaying
    public async Task UpdateCurrentTrack(WebView2 webView, MediaViewModel media)
    {
        if (Source is null || webView.CoreWebView2.IsSuspended || !Source.IsMusic)
            return;

        LivelyMusicModel? model = null;
        if (media.Thumbnail != null)
        {
            using var thumbnailSource = await media.GetThumbnailSourceAsync();
            var base64 = thumbnailSource != null ? await ReadToBase64Async(thumbnailSource) : string.Empty;
            model = new LivelyMusicModel
            {
                Title = media.Name,
                Artist = media.MainArtist?.Name,
                Thumbnail = base64,
                // Optional: Complete the mapping.
            };
        }

        await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(model));
    }

    private static async Task<string> ReadToBase64Async(IRandomAccessStream source)
    {
        using var stream = source.CloneStream();

        var buffer = new byte[stream.Size];
        await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
        return Convert.ToBase64String(buffer);
    }
}
