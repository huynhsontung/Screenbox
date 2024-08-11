#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Screenbox.Core.Helpers;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Screenbox.Core.ViewModels;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWallpaperSelectorViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>
{
    public ObservableCollection<LivelyWallpaperModel> Visualizers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private LivelyWallpaperModel? _selectedVisualizer;

    public static readonly LivelyWallpaperModel Default = new()
    {
        IsPreset = true,
        Path = string.Empty,
        Model = new LivelyInfoModel { Title = "Default" }
    };

    private readonly ILivelyWallpaperService _wallpaperService;
    private readonly IFilesService _filesService;
    private readonly ISettingsService _settingsService;

    public LivelyWallpaperSelectorViewModel(ILivelyWallpaperService wallpaperService, IFilesService filesService, ISettingsService settingsService)
    {
        _wallpaperService = wallpaperService;
        _filesService = filesService;
        _settingsService = settingsService;

        _selectedVisualizer = Default;
    }

    public async Task InitializeVisualizers()
    {
        var availableVisualizers = await _wallpaperService.GetAvailableVisualizersAsync();
        availableVisualizers.Insert(0, Default);
        Visualizers.SyncItems(availableVisualizers);

        if (WebView2Util.IsWebViewAvailable())
        {
            // Optional: Load previously selected visualizer from save by using the unique Path.
            // If NULL wallpaper visibility will be hidden but WebView process state will be based on x:Load=AudioOnly property.
            SelectedVisualizer =
                Visualizers.FirstOrDefault(visualizer => string.Equals(visualizer.Path,
                    _settingsService.LivelyActivePath, StringComparison.OrdinalIgnoreCase)) ??
                Visualizers[0];
        }
        else
        {
            // Optional: Prompt user to install WebView2 and display error message.
            // await WebView2Util.DownloadWebView();
        }
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        SelectedVisualizer = message.NewValue;
    }

    partial void OnSelectedVisualizerChanged(LivelyWallpaperModel? value)
    {
        // Ignore null value. Null is only a temporary value
        if (value == null) return;
        _settingsService.LivelyActivePath = value.Path;
    }

    [RelayCommand]
    private async Task OpenWallpaperLocation(LivelyWallpaperModel model)
    {
        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(model.Path);
            if (folder is not null)
                await Launcher.LaunchFolderAsync(folder);
        }
        catch
        {
            // Optional: Show error msg.
        }
    }

    [RelayCommand]
    private async Task BrowseVisualizer()
    {
        IReadOnlyList<StorageFile>? files = await _filesService.PickMultipleFilesAsync(".zip");
        if (files is null || files.Count == 0)
            return;

        if (files.Count > 1)
        {
            foreach (var file in files)
                await InstallVisualizer(file);
        }
        else
        {
            // Optional: Ask for user confirmation before install.
            var model = await InstallVisualizer(files[0]);
            if (model is not null)
                SelectedVisualizer = model;
        }
    }

    private async Task<LivelyWallpaperModel?> InstallVisualizer(StorageFile wallpaperFile)
    {
        var wallpaperModel = await _wallpaperService.InstallVisualizerAsync(wallpaperFile);
        if (wallpaperModel != null) Visualizers.Add(wallpaperModel);
        return wallpaperModel;
    }

    private bool CanDeleteVisualizer(LivelyWallpaperModel? visualizer) => visualizer is { IsPreset: false };

    [RelayCommand(CanExecute = nameof(CanDeleteVisualizer))]
    private async Task DeleteVisualizer(LivelyWallpaperModel? visualizer)
    {
        if (visualizer is null || visualizer.IsPreset)
            return;

        if (SelectedVisualizer == visualizer)
            SelectedVisualizer = Visualizers.FirstOrDefault();
        Visualizers.Remove(visualizer);

        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(visualizer.Path);
            await folder.DeleteAsync();
        }
        catch (Exception ex)
        {
            // Show error.
        }
    }
}
