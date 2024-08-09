using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Screenbox.Controls;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWallpaperSelector : UserControl
{
    internal LivelyWallpaperSelectorViewModel ViewModel => (LivelyWallpaperSelectorViewModel)DataContext;

    public LivelyWallpaperSelector()
    {
        this.InitializeComponent();
        this.DataContext = Ioc.Default.GetRequiredService<LivelyWallpaperSelectorViewModel>();
    }

    private async void LivelyWallpaperSelector_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeVisualizers();
    }
}
