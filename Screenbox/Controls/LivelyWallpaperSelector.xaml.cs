using CommunityToolkit.Mvvm.Input;
using Screenbox.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWallpaperSelector : UserControl
{
    public LivelyWallpaperModel Selection
    {
        get { return (LivelyWallpaperModel)GetValue(SelectionProperty); }
        set { SetValue(SelectionProperty, value); }
    }

    public static readonly DependencyProperty SelectionProperty =
        DependencyProperty.Register("Selection", typeof(LivelyWallpaperModel), typeof(LivelyWallpaperSelector), new PropertyMetadata(null));

    public ObservableCollection<LivelyWallpaperModel> Wallpapers
    {
        get { return (ObservableCollection<LivelyWallpaperModel>)GetValue(WallpapersProperty); }
        set { SetValue(WallpapersProperty, value); }
    }

    public static readonly DependencyProperty WallpapersProperty =
        DependencyProperty.Register("Wallpapers", typeof(ObservableCollection<LivelyWallpaperModel>), typeof(LivelyWallpaperSelector), new PropertyMetadata(null));

    public ICommand OpenWallpaperCommand
    {
        get { return (ICommand)GetValue(OpenWallpaperCommandProperty); }
        set { SetValue(OpenWallpaperCommandProperty, value); }
    }

    public static readonly DependencyProperty OpenWallpaperCommandProperty =
        DependencyProperty.Register("OpenWallpaperCommand", typeof(ICommand), typeof(LivelyWallpaperSelector), new PropertyMetadata(null));

    public ICommand DeleteWallpaperCommand
    {
        get { return (ICommand)GetValue(DeleteWallpaperCommandProperty); }
        set { SetValue(DeleteWallpaperCommandProperty, value); }
    }

    public static readonly DependencyProperty DeleteWallpaperCommandProperty =
        DependencyProperty.Register("DeleteWallpaperCommand", typeof(ICommand), typeof(LivelyWallpaperSelector), new PropertyMetadata(null));

    public RelayCommand<LivelyWallpaperModel> OpenWallpaperLocationCommand { get; } = new(async (model) => await OpenWallpaperLocation(model));

    public LivelyWallpaperSelector()
    {
        this.InitializeComponent();
        this.DataContext = this;
    }

    private static async Task OpenWallpaperLocation(LivelyWallpaperModel model)
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
}
