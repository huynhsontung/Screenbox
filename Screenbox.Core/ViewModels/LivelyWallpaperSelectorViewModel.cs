#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
public sealed partial class LivelyWallpaperSelectorViewModel : ObservableRecipient
{
    public ObservableCollection<LivelyWallpaperModel> Visualizers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private LivelyWallpaperModel? _selectedVisualizer;

    private readonly ILivelyWallpaperService _wallpaperService;
    private readonly IFilesService _filesService;

    public LivelyWallpaperSelectorViewModel(ILivelyWallpaperService wallpaperService, IFilesService filesService)
    {
        _wallpaperService = wallpaperService;
        _filesService = filesService;
    }

    public async Task InitializeVisualizers()
    {
        var availableVisualizers = await _wallpaperService.GetAvailableVisualizersAsync();

        Visualizers.Clear();
        foreach (var wallpaper in availableVisualizers)
        {
            Visualizers.Add(wallpaper);
        }

        if (WebView2Util.IsWebViewAvailable())
        {
            // Optional: Load previously selected visualizer from save by using the unique Path.
            // If NULL wallpaper visibility will be hidden but WebView process state will be based on on x:Load=AudioOnly property.
            SelectedVisualizer = Visualizers.FirstOrDefault();
        }
        else
        {
            // Optional: Prompt user to install WebView2 and display error message.
            // await WebView2Util.DownloadWebView();
        }
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
    private async Task OpenVisualizer()
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
