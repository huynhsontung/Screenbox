#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Screenbox.Controls;
using Screenbox.Core.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

    private async void HeaderCreateButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? playlistName = await CreatePlaylistDialog.GetPlaylistNameAsync();
        if (!string.IsNullOrWhiteSpace(playlistName))
        {
            await ViewModel.CreatePlaylistAsync(playlistName!);
        }
    }

    [RelayCommand]
    private async Task RenamePlaylistAsync(PlaylistViewModel playlist)
    {
        RenamePlaylistDialog dialog = new(playlist.Name);
        string? newName = await dialog.GetPlaylistNameAsync();
        if (!string.IsNullOrWhiteSpace(newName) && newName != playlist.Name)
        {
            await ViewModel.RenamePlaylistAsync(playlist, newName!);
        }
    }

    [RelayCommand]
    private async Task DeletePlaylistAsync(PlaylistViewModel playlist)
    {
        var deleteConfirmation = new DeletePlaylistDialog(playlist.Name);
        var result = await deleteConfirmation.ShowAsync();
        if (result == ContentDialogResult.Primary)
            await ViewModel.DeletePlaylistAsync(playlist);
    }
}
