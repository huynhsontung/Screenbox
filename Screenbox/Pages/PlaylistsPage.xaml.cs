#nullable enable

using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Screenbox.Controls;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlaylistsPage : Page
{
    internal PlaylistsPageViewModel ViewModel => (PlaylistsPageViewModel)DataContext;

    internal CommonViewModel Common { get; }

    public PlaylistsPage()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PlaylistsPageViewModel>();
        Common = Ioc.Default.GetRequiredService<CommonViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadPlaylistsAsync();
    }

    private async void HeaderCreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (!string.IsNullOrWhiteSpace(playlistName))
        {
            await ViewModel.CreatePlaylistAsync(playlistName!);
        }
    }

    private async void RenamePlaylistMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlaylistViewModel playlist }) return;

        RenamePlaylistDialog dialog = new(playlist.Caption);
        string? newName = await dialog.GetPlaylistNameAsync();
        if (!string.IsNullOrWhiteSpace(newName) && newName != playlist.Caption)
        {
            await ViewModel.RenamePlaylistAsync(playlist, newName);
        }
    }

    private async void DeletePlaylistMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PlaylistViewModel playlist }) return;

        ContentDialog dialog = new()
        {
            Title = "Delete Playlist",
            Content = $"Are you sure you want to delete '{playlist.Caption}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        dialog.FlowDirection = Helpers.GlobalizationHelper.GetFlowDirection();
        dialog.RequestedTheme = ((FrameworkElement)Windows.UI.Xaml.Window.Current.Content).RequestedTheme;

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeletePlaylistAsync(playlist);
        }
    }
}
